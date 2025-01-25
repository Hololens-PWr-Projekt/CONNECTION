using System;
using System.IO;
using System.Threading.Tasks;
using Hololens.Assets.Scripts.Connection.Model;
using Hololens.Assets.Scripts.Connection.Utils;
using MessagePack;
using UnityEngine;
#if ENABLE_WINMD_SUPPORT
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
#endif

namespace Hololens.Assets.Scripts.Connection.Manager
{
    public class WebSocketManager
    {
        private readonly string _endpoint;
        private AdvancedLogger _logger;

#if ENABLE_WINMD_SUPPORT
        private StreamWebSocket _webSocket;
        private DataWriter _dataWriter;
        private DataReader _dataReader;
#endif

        public WebSocketManager(string endpoint)
        {
            string logDirectoryPath = Path.Combine(Application.persistentDataPath, "ServerWebRTC_Logs");
            _logger = new AdvancedLogger(logDirectoryPath);
            _endpoint = endpoint;

            _logger.Log($"WebSocketManager initialized with endpoint: {_endpoint}");
        }

        // Connect to the WebSocket server
        public async Task ConnectAsync()
        {
#if ENABLE_WINMD_SUPPORT
            try
            {
                _logger.Log($"Attempting to connect to {_endpoint}...");
                _webSocket = new StreamWebSocket();
                await _webSocket.ConnectAsync(new Uri(_endpoint));
                _dataWriter = new DataWriter(_webSocket.OutputStream);
                _dataReader = new DataReader(_webSocket.InputStream);

                await _logger.LogAsync($"Successfully connected to {_endpoint}.");
            }
            catch (Exception ex)
            {
                await _logger.LogAsync($"Failed to connect to {_endpoint}: {ex.ToString()}");
                throw;
            }
#else
            Debug.LogWarning("ConnectAsync is not supported outside UWP.");
            _logger.Log("ConnectAsync called but is not supported outside UWP.");
            await Task.CompletedTask;
#endif
        }

        // Send a packet through the WebSocket
        public async Task SendAsync(Packet packet)
        {
#if ENABLE_WINMD_SUPPORT
            try
            {
                _logger.Log($"Preparing to send packet: {packet.PacketId}...");
                byte[] serializedData = MessagePackSerializer.Serialize(packet);
                _dataWriter.WriteBytes(serializedData);
                await _dataWriter.StoreAsync();
                await _logger.LogAsync($"Successfully sent packet: {packet.PacketId}");
            }
            catch (Exception ex)
            {
                await _logger.LogAsync($"Error sending packet {packet.PacketId}: {ex.ToString()}");
                throw;
            }
#else
            Debug.LogWarning("SendAsync is not supported outside UWP.");
            _logger.Log("SendAsync called but is not supported outside UWP.");
            await Task.CompletedTask;
#endif
        }

        // Receive a packet from the WebSocket
        public async Task ReceiveAsync(Action<Packet> onPacketReceived)
        {
#if ENABLE_WINMD_SUPPORT
            byte[] buffer = new byte[AppConfig.PACKET_BUFFER_BYTES];
            try
            {
                _logger.Log("Waiting to receive a message...");
                uint size = await _dataReader.LoadAsync((uint)buffer.Length);

                if (size == 0)
                {
                    await _logger.LogAsync("WebSocket closed by server.");
                    return;
                }

                _dataReader.ReadBytes(buffer);
                Packet packet = MessagePackSerializer.Deserialize<Packet>(buffer);

                _logger.Log($"Received packet: {packet.PacketId}");
                onPacketReceived?.Invoke(packet);
            }
            catch (Exception ex)
            {
                await _logger.LogAsync($"Error receiving message: {ex.ToString()}");
                throw;
            }
#else
            Debug.LogWarning("ReceiveAsync is not supported outside UWP.");
            _logger.Log("ReceiveAsync called but is not supported outside UWP.");
            await Task.CompletedTask;
#endif
        }

        // Close the WebSocket connection
        public async Task CloseAsync()
        {
#if ENABLE_WINMD_SUPPORT
            if (_webSocket != null)
            {
                try
                {
                    _logger.Log($"Closing WebSocket connection to {_endpoint}...");
                    _dataWriter?.DetachStream();
                    _dataWriter?.Dispose();
                    _dataReader?.Dispose();
                    _webSocket.Dispose();
                    _webSocket = null;
                    await _logger.LogAsync($"Successfully closed connection to {_endpoint}.");
                }
                catch (Exception ex)
                {
                    await _logger.LogAsync($"Error closing WebSocket connection to {_endpoint}: {ex.ToString()}");
                    throw;
                }
            }
            else
            {
                _logger.Log("CloseAsync called but no active WebSocket connection exists.");
            }
#else
            Debug.LogWarning("CloseAsync is not supported outside UWP.");
            _logger.Log("CloseAsync called but is not supported outside UWP.");
            await Task.CompletedTask;
#endif
        }

        // Check if the WebSocket is open
        public bool IsWebSocketOpen()
        {
#if ENABLE_WINMD_SUPPORT
            bool isOpen = _webSocket != null;
            _logger.Log($"IsWebSocketOpen called. Result: {isOpen}");
            return isOpen;
#else
            Debug.LogWarning("IsWebSocketOpen is not supported outside UWP.");
            _logger.Log("IsWebSocketOpen called but is not supported outside UWP.");
            return false;
#endif
        }
    }
}
