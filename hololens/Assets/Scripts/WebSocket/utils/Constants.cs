namespace Utils.Constants
{
    public static class Constants
    {
        // SERVER
        public const string WEBSOCKET_URL = "ws://localhost:8080/ws/hololens";

        // DATA
        public const string VERTICES = "vertices";
        public const string TRIANGLES = "triangles";
        public const string HANDS = "hands";

        // INTERVALS
        public const float RECONNECTION_INTERVAL = 5f;
        public const float HANDS_FRAMERATE_HZ = 60f;

        // MAX SIZE
        public const int MAX_CHUNK_BYTES = 1 * 1024 * 1024;
    }
}