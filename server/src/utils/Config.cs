namespace Server.Src.Utils
{
    public static class AppConfig
    {
        public const string SERVER_ADDR = "http://ws:localhost:8080";

        // PACKET_BUFFER must be greater than CHUNK_BUFFER
        public static readonly int CHUNK_BUFFER_SIZE = 128 * 1024;
        public static readonly int PACKET_BUFFER_BYTES = CHUNK_BUFFER_SIZE + 20;
    }
}
