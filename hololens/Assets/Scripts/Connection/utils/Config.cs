using System.Collections.Generic;
using Hololens.Assets.Scripts.Connection.Manager;
using UnityEngine;
#if ENABLE_WINMD_SUPPORT
using Windows.Storage;
#endif

namespace Hololens.Assets.Scripts.Connection.Utils
{
    public static class AppConfig
    {
        public static readonly List<ChannelConfig> CHANNELS_CONFIGS = new()
        {
            new ChannelConfig("mesh", "ws://localhost:8080/mesh"),
            new ChannelConfig("hands", "ws://localhost:8080/hands"),
        };

#if ENABLE_WINMD_SUPPORT
        // Use UWP-specific ApplicationData for paths
        public static readonly string WATCHED_DATA_PATH = ApplicationData.Current.LocalFolder.Path;
#else
        // Default for non-UWP platforms
        public static readonly string WATCHED_DATA_PATH = Application.persistentDataPath;
#endif

        public static readonly float GRACE_PERIOD = 10f;
        public static readonly int CHUNK_BUFFER_SIZE = 128 * 1024;
        public static readonly int PACKET_BUFFER_BYTES = CHUNK_BUFFER_SIZE + 20;
    }
}
