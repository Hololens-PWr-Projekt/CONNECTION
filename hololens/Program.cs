using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace HelloWorld
{
    class WebSocketClient
    { 
        public static async Task Main(string[] args)
        {
            using (var client = new ClientWebSocket())
            {
                // Connect to the WebSocket server
                Uri serverUri = new Uri("ws://localhost:8765");
                await client.ConnectAsync(serverUri, CancellationToken.None);
                Console.WriteLine("Connected to WebSocket server!");

                // Read JSON file
                string jsonFilePath = "mesh_1.json";
                string jsonString = await File.ReadAllTextAsync(jsonFilePath);

                // Send the JSON content
                var messageBuffer = Encoding.UTF8.GetBytes(jsonString);
                await client.SendAsync(new ArraySegment<byte>(messageBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
                Console.WriteLine("JSON file content sent!");

                // Close the connection
                await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                Console.WriteLine("WebSocket connection closed.");
            }
        }
    }
}