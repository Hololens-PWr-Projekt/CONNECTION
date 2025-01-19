using System;
using MessagePack;

namespace Server.Src.Model
{
    [MessagePackObject]
    public class Packet
    {
        [Key(0)]
        public string PacketId { get; set; }

        [Key(1)]
        public Channel Channel { get; set; }

        [Key(2)]
        public DateTime Timestamp { get; set; }

        [Key(3)]
        public Chunk Chunk { get; set; }

        public Packet() { }

        public Packet(string packetId, Channel channel, Chunk chunk)
        {
            this.PacketId = packetId;
            this.Channel = channel;
            this.Timestamp = DateTime.UtcNow;
            this.Chunk = chunk;
        }
    }

    [MessagePackObject]
    public class Chunk
    {
        [Key(0)]
        public int SequenceNumber { get; set; }

        [Key(1)]
        public int TotalChunks { get; set; }

        [Key(2)]
        public byte[] Data { get; set; }

        public Chunk(int sequenceNumber, int totalChunks, byte[] data)
        {
            this.SequenceNumber = sequenceNumber;
            this.TotalChunks = totalChunks;
            this.Data = data;
        }

        public Chunk() { }
    }

    public enum Channel
    {
        MESH,
        HANDS,
    }
}
