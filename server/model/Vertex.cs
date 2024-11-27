using Newtonsoft.Json;
using System;

namespace Server.Model
{
    [Serializable]
    public class Vertex(float x, float y, float z)
    {
        [JsonProperty("x")]
        public float X { get; set; } = x;

        [JsonProperty("y")]
        public float Y { get; set; } = y;

        [JsonProperty("z")]
        public float Z { get; set; } = z;
    }
}
