using System;
using Newtonsoft.Json;

namespace Server.Model
{
    [Serializable]
    public class Packet
    {
        [JsonProperty("packetId")]
        public string PacketId { get; }

        [JsonProperty("packetType")]
        public string PacketType { get; }

        [JsonProperty("chunk")]
        public Chunk Chunk { get; }

        [JsonProperty("metadata")]
        public Metadata Metadata { get; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; }

        public Packet(string packetId, string packetType, Chunk chunk, Metadata metadata, DateTime? timestamp = null)
        {
            PacketId = packetId;
            PacketType = packetType;
            Chunk = chunk;
            Metadata = metadata;
            Timestamp = timestamp ?? DateTime.UtcNow;
        }
    }

    [Serializable]
    public class Chunk
    {
        [JsonProperty("sequenceNumber")]
        public int SequenceNumber { get; }

        [JsonProperty("totalChunks")]
        public int TotalChunks { get; }

        [JsonProperty("data")]
        public string Data { get; }

        public Chunk(int sequenceNumber, int totalChunks, string data)
        {
            if (totalChunks <= 0)
            {
                throw new ArgumentException(
                    "Total chunks must be greater than 0.",
                    nameof(totalChunks)
                );
            }

            if (sequenceNumber < 1 || sequenceNumber > totalChunks)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sequenceNumber),
                    "Sequence number must be within the range of total chunks."
                );
            }

            SequenceNumber = sequenceNumber;
            TotalChunks = totalChunks;
            Data = data;
        }
    }

    [Serializable]
    public class Metadata
    {
        [JsonProperty("chunkBytes")]
        public int ChunkBytes { get; }

        public Metadata(int chunkBytes)
        {
            if (chunkBytes <= 0)
            {
                throw new ArgumentException(
                    "Chunk size must be greater than 0.",
                    nameof(chunkBytes)
                );
            }

            ChunkBytes = chunkBytes;
        }
    }
}
