using System;
using System.Collections.Generic;
using System.Text;
using Manager.Json;
using Model.Packet;

namespace Utils.Packets
{
    using Utils.Constants;
    
    public static class PacketUtils
    {
        public static List<Packet> Split(string packetId, string type, object payload)
        {
            string jsonData = JsonManager.Serialize(payload);

            List<Packet> packets = new();
            List<string> chunks = SplitIntoChunks(jsonData);
            int totalChunks = chunks.Count;

            for (int i = 0; i < totalChunks; i++)
            {
                Chunk chunk = new(i + 1, totalChunks, chunks[i]);
                int chunk_bytes = Encoding.UTF8.GetByteCount(chunks[i]);
                Metadata metadata = new(chunk_bytes);

                packets.Add(new Packet(packetId, type, chunk, metadata));
            }

            return packets;
        }

        public static List<Packet> CreatePacket(string packetId, string type, string jsonData)
        {
            // Needs refactor
            List<Packet> singlePacket = new();

            Chunk chunk = new(1, 1, jsonData);
            int chunkBytes = Encoding.UTF8.GetByteCount(jsonData);
            Metadata metadata = new(chunkBytes);

            singlePacket.Add(new Packet(packetId, type, chunk, metadata));

            return singlePacket;
        }

        private static List<string> SplitIntoChunks(string data)
        {
            // TODO: chaning 1 to other number causes deserialize error in server, problem jest przy deserializacji Packet, coś tam jest zle zakonczone
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            List<string> chunks = new();
            int dataLength = dataBytes.Length;

            for (int i = 0; i < dataLength; i += Constants.MAX_CHUNK_BYTES)
            {
                int length = Math.Min(Constants.MAX_CHUNK_BYTES, dataLength - i);
                chunks.Add(Encoding.UTF8.GetString(dataBytes, i, length));
            }

            return chunks;
        }
    }
}
