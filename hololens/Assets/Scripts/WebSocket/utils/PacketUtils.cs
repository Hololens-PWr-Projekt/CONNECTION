using System;
using System.Text;
using System.Collections.Generic;
using Model.Packet;
using Manager.Json;
using UnityEngine;

namespace Utils.Packets
{
    public static class PacketUtils
    {
        public static List<Packet> Split(string packetId, string type, object payload)
        {
            string jsonData = JsonManager.Serialize(payload);

            List<Packet> packets = new();
            List<string> chunks = SplitIntoChunks(jsonData);
            int totalChunks = chunks.Count;

            DateTime timestamp = DateTime.Now;

            for (int i = 0; i < totalChunks; i++)
            {
                Chunk chunk = new(i + 1, totalChunks, chunks[i]);
                int chunk_bytes = Encoding.Unicode.GetByteCount(chunks[i]);
                Metadata metadata = new(chunk_bytes);

                // Pass the timestamp to the Packet constructor
                packets.Add(new Packet(packetId, type, chunk, metadata, timestamp));
            }

            return packets;
        }

        private static List<string> SplitIntoChunks(string data)
        {
            // TODO: chaning 1 to other number causes deserialize error in server, problem jest przy deserializacji Packet, coś tam jest zle zakonczone
            const int MAX_CHUNK_BYTES = 1 * 1024;

            byte[] dataBytes = Encoding.Unicode.GetBytes(data);
            Debug.Log(Encoding.Unicode.GetByteCount(data));
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
