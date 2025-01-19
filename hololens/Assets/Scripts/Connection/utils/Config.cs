using System.Collections.Generic;
using Hololens.Assets.Scripts.Connection.Manager;

namespace Hololens.Assets.Scripts.Connection.Utils
{
    public static class AppConfig
    {
        public static readonly List<ChannelConfig> CHANNELS_CONFIGS = new()
        {
            new ChannelConfig("mesh", "ws://localhost:8080/mesh"),
            new ChannelConfig("hands", "ws://localhost:8080/hands"),
        };

        // PACKET_BUFFER must be greater than CHUNK_BUFFER
        public static readonly int CHUNK_BUFFER_SIZE = 128 * 1024;
        public static readonly int PACKET_BUFFER_BYTES = CHUNK_BUFFER_SIZE + 20;
    }
}
