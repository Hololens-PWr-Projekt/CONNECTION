using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;
using Server.Src.Model;
using Server.Src.Utils;

namespace Server.Src.Manager
{
    public class WebSocketManager
    {
        private readonly WebSocket _webSocket;
        private static readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;

        public WebSocketManager(WebSocket webSocket)
        {
            _webSocket = webSocket;
        }

        public async Task<Packet?> ReceivePacketAsync()
        {
            byte[] buffer = _bufferPool.Rent(AppConfig.PACKET_BUFFER_BYTES);
            try
            {
                var receiveTask = await _webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None
                );

                if (receiveTask.MessageType == WebSocketMessageType.Binary)
                {
                    return MessagePackSerializer.Deserialize<Packet>(
                        buffer.AsSpan(0, receiveTask.Count).ToArray()
                    );
                }

                return null;
            }
            finally
            {
                _bufferPool.Return(buffer);
            }
        }

        public async Task SendAsync(Packet packet)
        {
            var serializedData = MessagePackSerializer.Serialize(packet);
            await _webSocket.SendAsync(
                new ArraySegment<byte>(serializedData),
                WebSocketMessageType.Binary,
                true, // message is complete
                CancellationToken.None
            );
        }

        public async Task CloseAsync()
        {
            await _webSocket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Server closing",
                CancellationToken.None
            );
        }
    }
}
