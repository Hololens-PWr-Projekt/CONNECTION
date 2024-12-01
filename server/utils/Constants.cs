namespace Server.Utils
{
    public static class Constants
    {
        public const string SERVER_URL = "http://localhost:8080/ws/hololens/";
        // This value MUST be higher then MAX_CHUNK_BYTES in hololens (triangles and vertices each has 1MiB chunks)
        // Max websocket buffer is 4 MiB
        public const int MAX_BUFFER_BYTES = 3 * 1024 * 1024;
        public const int BAD_REQUEST_CODE = 400;

     }

}