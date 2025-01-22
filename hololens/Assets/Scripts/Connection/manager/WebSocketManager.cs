using System;
using System.Threading.Tasks;
using Hololens.Assets.Scripts.Connection.Model;
using UnityEngine;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Hololens.Assets.Scripts.Connection.Manager
{
    public class WebSocketManager
    {
        private readonly string _endpoint;
        private MessageWebSocket _webSocket;
        private DataWriter _messageWriter;

        public WebSocketManager(string endpoint)
        {
            _endpoint = endpoint;
        }

        /// <summary>
        /// Asynchronously connects to the WebSocket server.
        /// </summary>
        public async Task ConnectAsync()
        {
            try
            {
                _webSocket = new MessageWebSocket
                {
                    Control = { MessageType = SocketMessageType.Binary },
                };
                _webSocket.MessageReceived += OnMessageReceived;

                await _webSocket.ConnectAsync(new Uri(_endpoint));
                _messageWriter = new DataWriter(_webSocket.OutputStream);

                Debug.Log($"Connected to {_endpoint}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to connect to {_endpoint}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Sends a packet to the WebSocket server.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        public async Task SendAsync(Packet packet)
        {
            if (_webSocket == null || _messageWriter == null)
            {
                Debug.LogError("WebSocket is not connected.");
                return;
            }

            try
            {
                byte[] serializedData = MessagePack.MessagePackSerializer.Serialize(packet);
                _messageWriter.WriteBytes(serializedData);
                await _messageWriter.StoreAsync();

                Debug.Log("Packet sent successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error sending packet: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Handles incoming messages from the WebSocket server.
        /// </summary>
        private async void OnMessageReceived(
            MessageWebSocket sender,
            MessageWebSocketMessageReceivedEventArgs args
        )
        {
            try
            {
                using (DataReader reader = args.GetDataReader())
                {
                    uint messageLength = reader.UnconsumedBufferLength;
                    byte[] buffer = new byte[messageLength];
                    reader.ReadBytes(buffer);

                    Packet packet = MessagePack.MessagePackSerializer.Deserialize<Packet>(buffer);
                    Debug.Log($"Received packet: {packet.PacketId}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error receiving message: {ex.Message}");
            }
        }

        /// <summary>
        /// Closes the WebSocket connection.
        /// </summary>
        public async Task CloseAsync()
        {
            if (_webSocket == null)
                return;

            try
            {
                await _webSocket.CloseAsync(
                    Windows.Networking.Sockets.SocketCloseStatus.NormalClosure,
                    "Closing"
                );
                _webSocket.Dispose();
                _webSocket = null;

                Debug.Log($"Connection to {_endpoint} closed.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error closing WebSocket connection: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Checks if the WebSocket connection is open.
        /// </summary>
        public bool IsWebSocketOpen()
        {
            return _webSocket != null;
        }
    }
}
