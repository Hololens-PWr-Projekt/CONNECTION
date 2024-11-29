using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Server.Utils;

namespace Server.Service
{
    public class WebSocketManager(
        Action<string> onMessageReceived,
        Action<string> onMessageSent,
        Action<Exception> onError
    ) : IDisposable
    {
        private HttpListener? httpListener;
        private WebSocket? webSocket;

        private readonly Action<string> onMessageReceived = onMessageReceived;
        private readonly Action<string> onMessageSent = onMessageSent;
        private readonly Action<Exception> onError = onError;

        public void StartServer()
        {
            Task.Run(async () =>
            {
                httpListener = new HttpListener();
                httpListener.Prefixes.Add(Constants.SERVER_URL);
                httpListener.Start();
                LogInfo("WebSocket server started successfully.");

                while (httpListener.IsListening)
                {
                    try
                    {
                        var context = await httpListener.GetContextAsync();

                        if (context.Request.IsWebSocketRequest)
                        {
                            await HandleWebSocketConnection(context);
                        }
                        else
                        {
                            context.Response.StatusCode = Constants.BAD_REQUEST_CODE;
                            context.Response.Close();
                        }
                    }
                    catch (Exception e)
                    {
                        onError(e);
                    }
                }
            });
        }

        private async Task HandleWebSocketConnection(HttpListenerContext context)
        {
            try
            {
                var webSocketContext = await context.AcceptWebSocketAsync(null);
                webSocket = webSocketContext.WebSocket;
                onMessageReceived("WebSocket connection established.");

                await ReceiveMessages();
            }
            catch (Exception e)
            {
                onError(e);
            }
        }

        private async Task ReceiveMessages()
        {
            byte[] buffer = new byte[Constants.WEBSOCKET_BUFFER_BYTES];

            while (IsConnectionOpen())
            {
                try
                {
                    var result = await webSocket!.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        CancellationToken.None
                    );

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await CloseConnection();
                        break;
                    }

                    var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    onMessageReceived(receivedMessage);

                    // await SendMessage($"Echo: {receivedMessage}");
                }
                catch (Exception e)
                {
                    onError(e);
                }
            }
        }

        public async Task SendMessage(string message)
        {
            if (IsConnectionClosed())
            {
                LogInfo("WebSocket is not open. Message not sent.");
                return;
            }

            try
            {
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                await webSocket!.SendAsync(
                    new ArraySegment<byte>(messageBytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
                onMessageSent($"Sent: {message}");
            }
            catch (Exception e)
            {
                onError(e);
            }
        }

        private async Task CloseConnection()
        {
            if (IsConnectionClosed())
            {
                LogInfo("Websocket connection is already closed!");
                return;
            }

            try
            {
                await webSocket!.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Closing",
                    CancellationToken.None
                );
            }
            catch (Exception e)
            {
                onError(e);
            }
            finally
            {
                webSocket!.Dispose();
                webSocket = null;
            }
        }

        public void StopServer()
        {
            httpListener?.Stop();
            CloseConnection().Wait();
            LogInfo("WebSocket server stopped.");
        }

        public void Dispose()
        {
            StopServer();
            httpListener?.Close();
        }

        public bool IsConnectionClosed() => webSocket?.State == WebSocketState.Closed;

        private bool IsConnectionOpen() => webSocket?.State == WebSocketState.Open;

        private void LogInfo(string message) => onMessageReceived($"Info: {message}");
    }
}
