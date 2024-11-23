using System.Collections.Generic;
using System;
using System.Text;
using Model.Packet;
using Manager.Json;

namespace Utils.Packets;
public static class PacketUtils
{
    private const int MAX_CHUNK_BYTES = 1024;

    public static List<Packet> SplitData(string packetId, PacketType type, string data)
    {
        List<Packet> packets = new();

        string jsonData = JsonManager.Serialize(data);
        byte[] dataBytes = Encoding.UTF8.GetBytes(jsonData);
        int totalPackets = (int)Math.Ceiling((double)dataBytes.Length / MAX_CHUNK_BYTES);
        int totalSize = dataBytes.Length;

        for (int i = 0; i < totalPackets; i++)
        {
            int start = i * MAX_CHUNK_BYTES;
            int length = Math.Min(MAX_CHUNK_BYTES, totalSize - start);

            // Extract the byte chunk
            byte[] chunkBytes = new byte[length];
            Array.Copy(dataBytes, start, chunkBytes, 0, length);

            // Convert byte chunk back to string for packet
            string chunk = Encoding.UTF8.GetString(chunkBytes);
            bool isLast = i == totalPackets - 1;

            packets.Add(new Packet(packetId, type, i, isLast, chunk));
        }

        return packets;
    }
}
