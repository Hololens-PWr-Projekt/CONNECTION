using Newtonsoft.Json;
using System;

namespace Server.Model
{
    [Serializable]
    public class Vertex
    {
        [JsonProperty("x")]
        public float X { get; }

        [JsonProperty("y")]
        public float Y { get; }

        [JsonProperty("z")]
        public float Z { get; }

        public Vertex(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
