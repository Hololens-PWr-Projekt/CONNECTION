using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace server
{
    public partial class MainWindow : Window
    {
        private HttpListener? _httpListener;
        private readonly string _serverUri = "http://localhost:8080/ws/hololens/"; // Change to HTTP for WebSocket upgrade
        private const int WebSocketBufferSize = 1024;

        public MainWindow()
        {
            InitializeComponent();
        }

        // Start the HTTP listener to accept WebSocket connections
        private async void StartServerButton_Click(object sender, RoutedEventArgs e)
        {
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add(_serverUri + "/");
            _httpListener.Start();
            MessagesTextBox.Text += $"\nListening for connections on {_serverUri}";

            await AcceptConnectionsAsync();
        }

        // Accept incoming WebSocket connections
        private async Task AcceptConnectionsAsync()
        {
            while (true)
            {
                try
                {
                    var context = await _httpListener.GetContextAsync();
                    if (context.Request.IsWebSocketRequest)
                    {
                        var webSocketContext = await context.AcceptWebSocketAsync(null);
                        MessagesTextBox.Text += "\nWebSocket connection established.";

                        // Handle WebSocket communication
                        var webSocket = webSocketContext.WebSocket;
                        await ReceiveMessagesAsync(webSocket);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessagesTextBox.Text += $"\nError accepting connection: {ex.Message}";
                }
            }
        }

        // Receive messages from the connected WebSocket
        private async Task ReceiveMessagesAsync(WebSocket webSocket)
        {
            var buffer = new byte[WebSocketBufferSize];

            while (webSocket.State == WebSocketState.Open)
            {
                try
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    MessagesTextBox.Text += $"\nReceived: {receivedMessage}";

                    // Echo the received message back
                    await SendMessageAsync(webSocket, $"Echo: {receivedMessage}");
                }
                catch (Exception ex)
                {
                    MessagesTextBox.Text += $"\nError receiving message: {ex.Message}";
                }
            }
        }

        // Send a message back to the WebSocket client
        private async Task SendMessageAsync(WebSocket webSocket, string message)
        {
            var messageBytes = Encoding.UTF8.GetBytes(message);
            await webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
            MessagesTextBox.Text += $"\nSent: {message}";
        }

        // Cleanup when the window is closed
        protected override async void OnClosed(EventArgs e)
        {
            _httpListener?.Stop();
            MessagesTextBox.Text += "\nServer stopped.";
            base.OnClosed(e);
        }
    }
}
