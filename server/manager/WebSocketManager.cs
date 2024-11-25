using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Manager
{
    public class WebSocketManager
    {
        private const string SERVER_URI = "http://localhost:8080/ws/hololens/";
        private const int WEBSOCKET_BUFFER_BYTES = 1024;

        private HttpListener? _httpListener;
        private WebSocket? _webSocket;

        private readonly Action<string> _onMessageReceived;
        private readonly Action<string> _onMessageSent;  // New callback for sending messages
        private readonly Action<Exception> _onError;

        public WebSocketManager(Action<string> onMessageReceived, Action<string> onMessageSent, Action<Exception> onError)
        {
            _onMessageReceived = onMessageReceived;
            _onMessageSent = onMessageSent;  // Initialize callback
            _onError = onError;
        }

        public void StartServer()
        {
            Task.Run(async () =>
            {
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add(SERVER_URI);
                _httpListener.Start();

                while (true)
                {
                    try
                    {
                        var context = await _httpListener.GetContextAsync();

                        if (context.Request.IsWebSocketRequest)
                        {
                            var webSocketContext = await context.AcceptWebSocketAsync(null);
                            _webSocket = webSocketContext.WebSocket;
                            _onMessageReceived("WebSocket connection established.");

                            await ReceiveMessages();
                        }
                        else
                        {
                            context.Response.StatusCode = 400;
                            context.Response.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        _onError(ex);
                    }
                }
            });
        }

        private async Task ReceiveMessages()
        {
            var buffer = new byte[WEBSOCKET_BUFFER_BYTES];

            while (_webSocket?.State == WebSocketState.Open)
            {
                try
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _onMessageReceived(receivedMessage);

                    // Echo the received message back
                    await SendMessage($"Echo: {receivedMessage}");
                }
                catch (Exception ex)
                {
                    _onError(ex);
                }
            }
        }

        public async Task SendMessage(string message)
        {
            if (_webSocket?.State == WebSocketState.Open)
            {
                var messageBytes = Encoding.UTF8.GetBytes(message);
                await _webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);

                // Trigger the onMessageSent callback when the message is sent
                _onMessageSent($"Sent: {message}");
            }
        }

        public bool IsConnectionOpen()
        {
            return _webSocket?.State == WebSocketState.Open;
        }

        public void StopServer()
        {
            _httpListener?.Stop();
            _webSocket?.Abort();
            _webSocket = null;
        }
    }
}
