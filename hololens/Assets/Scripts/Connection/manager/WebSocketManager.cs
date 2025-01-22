using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Hololens.Assets.Scripts.Connection.Model;
using Hololens.Assets.Scripts.Connection.Utils;
using MessagePack;
using UnityEngine;

namespace Hololens.Assets.Scripts.Connection.Manager
{
    public class WebSocketManager
    {
        private readonly string _endpoint;
        private CancellationTokenSource _cancellationTokenSource;

#if UNITY_EDITOR
        private System.Net.WebSockets.ClientWebSocket _webSocket;
#elif WINDOWS_UWP
        private Windows.Networking.Sockets.MessageWebSocket _webSocket;
        private Windows.Storage.Streams.DataWriter _messageWriter;
#endif

        public WebSocketManager(string endpoint)
        {
            _endpoint = endpoint;
        }

        public IEnumerator ConnectAsync()
        {
            _cancellationTokenSource = new CancellationTokenSource();

#if UNITY_EDITOR
            _webSocket = new System.Net.WebSockets.ClientWebSocket();
            var connectTask = _webSocket.ConnectAsync(
                new Uri(_endpoint),
                _cancellationTokenSource.Token
            );

            while (!connectTask.IsCompleted)
            {
                yield return null;
            }

            if (connectTask.Exception != null)
            {
                Debug.LogError(
                    $"Failed to connect to {_endpoint}: {connectTask.Exception.Message}"
                );
            }
            else
            {
                Debug.Log($"Connected to {_endpoint}");
            }
#elif WINDOWS_UWP
            _webSocket = new Windows.Networking.Sockets.MessageWebSocket();
            _webSocket.Control.MessageType = Windows.Networking.Sockets.SocketMessageType.Binary;
            _webSocket.MessageReceived += OnMessageReceived;

            var connectTask = Task.Run(async () =>
            {
                try
                {
                    await _webSocket.ConnectAsync(new Uri(_endpoint));
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to connect to {_endpoint}: {ex.Message}");
                }
            });

            while (!connectTask.IsCompleted)
            {
                yield return null;
            }

            if (connectTask.Exception != null)
            {
                Debug.LogError(
                    $"Failed to connect to {_endpoint}: {connectTask.Exception.InnerException?.Message}"
                );
            }
            else
            {
                Debug.Log($"Connected to {_endpoint}");
                _messageWriter = new Windows.Storage.Streams.DataWriter(_webSocket.OutputStream);
            }
#endif
        }

        public IEnumerator SendAsync(Packet packet)
        {
            byte[] serializedData = MessagePackSerializer.Serialize(packet);

#if UNITY_EDITOR
            if (_webSocket == null || _webSocket.State != System.Net.WebSockets.WebSocketState.Open)
            {
                Debug.LogError("WebSocket is not connected.");
                yield break;
            }

            var sendTask = _webSocket.SendAsync(
                new ArraySegment<byte>(serializedData),
                System.Net.WebSockets.WebSocketMessageType.Binary,
                true,
                _cancellationTokenSource.Token
            );

            while (!sendTask.IsCompleted)
            {
                yield return null;
            }

            if (sendTask.Exception != null)
            {
                Debug.LogError($"Error sending message: {sendTask.Exception.Message}");
            }
#elif WINDOWS_UWP
            if (_webSocket == null || _messageWriter == null)
            {
                Debug.LogError("WebSocket is not connected.");
                yield break;
            }

            var sendTask = Task.Run(async () =>
            {
                try
                {
                    _messageWriter.WriteBytes(serializedData);
                    await _messageWriter.StoreAsync();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error sending message: {ex.Message}");
                }
            });

            while (!sendTask.IsCompleted)
            {
                yield return null;
            }

            if (sendTask.Exception != null)
            {
                Debug.LogError(
                    $"Error sending message: {sendTask.Exception.InnerException?.Message}"
                );
            }
#endif
        }

        public IEnumerator ReceiveAsync(Action<Packet> onPacketReceived)
        {
#if UNITY_EDITOR
            byte[] buffer = new byte[AppConfig.PACKET_BUFFER_BYTES];
            while (_webSocket.State == System.Net.WebSockets.WebSocketState.Open)
            {
                var receiveTask = _webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    _cancellationTokenSource.Token
                );

                while (!receiveTask.IsCompleted)
                {
                    yield return null;
                }

                if (receiveTask.Exception != null)
                {
                    Debug.LogError($"Error receiving message: {receiveTask.Exception.Message}");
                    yield break;
                }

                var result = receiveTask.Result;
                if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)
                {
                    Debug.LogWarning("WebSocket closed by server.");
                    yield break;
                }

                var packet = MessagePackSerializer.Deserialize<Packet>(
                    buffer.AsSpan(0, result.Count).ToArray()
                );
                onPacketReceived?.Invoke(packet);
            }
#elif WINDOWS_UWP
            // Message receiving is handled asynchronously in OnMessageReceived
            yield break;
#endif
        }

#if WINDOWS_UWP
        private void OnMessageReceived(
            Windows.Networking.Sockets.MessageWebSocket sender,
            Windows.Networking.Sockets.MessageWebSocketMessageReceivedEventArgs args
        )
        {
            Task.Run(async () =>
            {
                try
                {
                    using (var reader = args.GetDataReader())
                    {
                        uint messageLength = reader.UnconsumedBufferLength;
                        byte[] buffer = new byte[messageLength];
                        reader.ReadBytes(buffer);

                        var packet = MessagePackSerializer.Deserialize<Packet>(buffer);
                        UnityEngine.Debug.Log($"Message received: {packet}");
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Error reading message: {ex.Message}");
                }
            });
        }
#endif

        public IEnumerator CloseAsync()
        {
            if (_webSocket != null)
            {
#if UNITY_EDITOR
                var closeTask = _webSocket.CloseAsync(
                    System.Net.WebSockets.WebSocketCloseStatus.NormalClosure,
                    "Closing",
                    _cancellationTokenSource.Token
                );

                while (!closeTask.IsCompleted)
                {
                    yield return null;
                }

                if (closeTask.Exception != null)
                {
                    Debug.LogError($"Error closing WebSocket: {closeTask.Exception.Message}");
                }
#elif WINDOWS_UWP
                try
                {
                    _webSocket.Dispose();
                    _webSocket = null;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error closing WebSocket: {ex.Message}");
                }
#endif
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                Debug.Log($"Connection to {_endpoint} closed.");
            }
        }

        public bool IsWebSocketOpen()
        {
#if UNITY_EDITOR
            return _webSocket != null
                && _webSocket.State == System.Net.WebSockets.WebSocketState.Open;
#elif WINDOWS_UWP
            return _webSocket != null;
#endif
        }
    }
}
