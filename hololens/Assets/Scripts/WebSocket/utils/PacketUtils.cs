using System;
using System.Text;
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
        
        private static List<string> SplitIntoChunks(string data)
        {
            byte[] dataBytes = Encoding.Unicode.GetBytes(data);
            List<string> chunks = new();
            int dataLength = dataBytes.Length;

            for (int i = 0; i < dataLength; i += MAX_CHUNK_BYTES)
            {
                int length = Math.Min(MAX_CHUNK_BYTES, dataLength - i);
                chunks.Add(Encoding.Unicode.GetString(dataBytes, i, length));
            }

            return chunks;
        }
    }
}
