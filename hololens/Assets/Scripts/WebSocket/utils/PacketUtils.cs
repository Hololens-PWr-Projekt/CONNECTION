using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Model.Packet;
using Manager.Json;

namespace Utils.Packets
{
    public static class PacketUtils
    {
        private const int MAX_CHUNK_BYTES = 1024;

        public static List<Packet> Split(string packetId, PacketType type, object payload)
        {
            string jsonData = JsonManager.Serialize(payload);
            List<Packet> packets = new();
            List<string> chunks = SplitIntoChunks(jsonData);
            int totalChunks = chunks.Count;

            for (int i = 0; i < totalChunks; i++)
            {
                Chunk chunk = new(i + 1, totalChunks, chunks[i]);
                int chunk_bytes = Encoding.Unicode.GetByteCount(chunks[i]);
                Metadata metadata = new(chunk_bytes);

                packets.Add(new Packet(packetId, type, chunk, metadata));
            }

            return packets;
        }

        // The logic will go to desktop client
        public static string Reassemble(List<Packet> packets)
        {
            if (ArePacketsEmpty(packets))
            {
                throw new ArgumentException("Packets list cannot be null!");
            }

            string packetId = packets.First().PacketId;
            string packetType = packets.First().PacketType;

            if (ArePacektsSame(packets, packetId, packetType)) {
                throw new InvalidOperationException("All packets must have the same PacketId and PacketType.");
            }

            packets = packets.OrderBy(p => p.Chunk.SequenceNumber).ToList();
            string combinedData = string.Join("", packets.Select(p => p.Chunk.Data));

            return combinedData;
        }

        private static bool ArePacektsSame(List<Packet> packets, string packetId, string packetType)
        {
            return !packets.All(p => p.PacketId == packetId && p.PacketType == packetType);
        }

        private static bool ArePacketsEmpty(List<Packet> packets)
        {
            return packets == null || packets.Count == 0;
        }

        private static List<string> SplitIntoChunks(string data)
        {
            List<string> chunks = new();
            int totalLength = data.Length;

            for (int i = 0; i < totalLength; i += MAX_CHUNK_BYTES)
            {
                int length = Math.Min(MAX_CHUNK_BYTES, totalLength - i);
                chunks.Add(data.Substring(i, length));
            }

            return chunks;
        }
    }
}
