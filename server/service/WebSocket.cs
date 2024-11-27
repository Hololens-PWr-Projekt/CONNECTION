using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Service
{
    public class WebSocketManager(Action<string> onMessageReceived, Action<string> onMessageSent, Action<Exception> onError) : IDisposable
    {
        private const string SERVER_URI = "http://localhost:8080/ws/hololens/";
        private const int WEBSOCKET_BUFFER_BYTES = 1024;

        private HttpListener? _httpListener;
        private WebSocket? _webSocket;

        private readonly Action<string> _onMessageReceived = onMessageReceived;
        private readonly Action<string> _onMessageSent = onMessageSent;
        private readonly Action<Exception> _onError = onError;

        public void StartServer()
        {
            Task.Run(async () =>
            {
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add(SERVER_URI);
                _httpListener.Start();
                LogInfo("WebSocket server started successfully.");

                while (_httpListener.IsListening)
                {
                    try
                    {
                        var context = await _httpListener.GetContextAsync();

                        if (context.Request.IsWebSocketRequest)
                        {
                            await HandleWebSocketConnection(context);
                        }
                        else
                        {
                            context.Response.StatusCode = 400;
                            context.Response.Close();
                        }
                    }
                    catch (Exception e)
                    {
                        _onError(e);
                    }
                }
            });
        }

        private async Task HandleWebSocketConnection(HttpListenerContext context)
        {
            try
            {
                var webSocketContext = await context.AcceptWebSocketAsync(null);
                _webSocket = webSocketContext.WebSocket;
                _onMessageReceived("WebSocket connection established.");

                await ReceiveMessages();
            }
            catch (Exception e)
            {
                _onError(e);
            }
        }

        private async Task ReceiveMessages()
        {
            byte[] buffer = new byte[WEBSOCKET_BUFFER_BYTES];

            while (IsConnectionOpen())
            {
                try
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await CloseConnection();
                        break;
                    }

                    var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _onMessageReceived(receivedMessage);


                    // await SendMessage($"Echo: {receivedMessage}");
                }
                catch (Exception e)
                {
                    _onError(e);
                }
            }
        }

        public async Task SendMessage(string message)
        {
            if (IsConnectionOpen())
            {
                try
                {
                    byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                    await _webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                    _onMessageSent($"Sent: {message}");
                }
                catch (Exception e)
                {
                    _onError(e);
                }
            }
            else
            {
                LogInfo("WebSocket is not open. Message not sent.");
            }
        }

        public bool IsConnectionOpen() => _webSocket?.State == WebSocketState.Open;

        private void LogInfo(string message) => _onMessageReceived($"Info: {message}");

        private async Task CloseConnection()
        {
            if (IsConnectionOpen())
            {
                try
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    _webSocket.Dispose();
                    _webSocket = null;
                }
                catch (Exception e)
                {
                    _onError(e);
                }
            }
        }

        public void StopServer()
        {
            _httpListener?.Stop();
            CloseConnection().Wait();
            LogInfo("WebSocket server stopped.");
        }

        public void Dispose()
        {
            StopServer();
            _httpListener?.Close();
        }
    }
}
