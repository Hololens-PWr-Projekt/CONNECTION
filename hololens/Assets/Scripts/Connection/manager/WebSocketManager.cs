using System;
using System.Buffers;
using System.Collections;
using System.Net.WebSockets;
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
        private ClientWebSocket _webSocket;
        private static readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;

        public WebSocketManager(string endpoint)
        {
            _endpoint = endpoint;
        }

        public IEnumerator ConnectAsync()
        {
            _webSocket = new ClientWebSocket();
            var connectTask = _webSocket.ConnectAsync(new Uri(_endpoint), CancellationToken.None);

            while (!connectTask.IsCompleted)
            {
                // Wait until task is complete
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
        }

        public IEnumerator SendAsync(Packet packet)
        {
            byte[] seralizedData = MessagePackSerializer.Serialize(packet);
            Task sendTask = _webSocket.SendAsync(
                new ArraySegment<byte>(seralizedData),
                WebSocketMessageType.Binary,
                true,
                CancellationToken.None
            );

            while (!sendTask.IsCompleted)
            {
                yield return null;
            }

            if (sendTask.Exception != null)
            {
                Debug.LogError($"Error sending raw message: {sendTask.Exception.Message}");
            }
        }

        public IEnumerator ReceiveAsync(Action<Packet> onPacketReceived)
        {
            byte[] buffer = _bufferPool.Rent(AppConfig.PACKET_BUFFER_BYTES);
            try
            {
                Task<WebSocketReceiveResult> receiveTask = _webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None
                );

                while (!receiveTask.IsCompleted)
                {
                    yield return null;
                }

                if (receiveTask.Exception != null)
                {
                    Debug.LogError($"Error sending raw message: {receiveTask.Exception.Message}");
                    yield break;
                }
                if (receiveTask.Result.MessageType == WebSocketMessageType.Close)
                {
                    Debug.LogWarning("WebSocket closed by a server.");
                    yield break;
                }
                Packet packet = MessagePackSerializer.Deserialize<Packet>(
                    buffer.AsSpan(0, receiveTask.Result.Count).ToArray()
                );
                onPacketReceived?.Invoke(packet);
            }
            finally
            {
                _bufferPool.Return(buffer);
            }
        }

        public IEnumerator CloseAsync()
        {
            if (_webSocket.State == WebSocketState.Open)
            {
                var closeTask = _webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Closing",
                    CancellationToken.None
                );

                while (!closeTask.IsCompleted)
                {
                    // Wait until the task is complete
                    yield return null;
                }

                if (closeTask.Exception != null)
                {
                    Debug.LogError($"Error closing connection: {closeTask.Exception.Message}");
                }
                else
                {
                    Debug.Log($"Connection to {_endpoint} closed.");
                }

                _webSocket.Dispose();
                _webSocket = null;
            }
        }

        public bool IsWebSocketOpen()
        {
            return _webSocket.State == WebSocketState.Open;
        }
    }
}
