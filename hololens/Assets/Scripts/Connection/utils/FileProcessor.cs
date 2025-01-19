using System;
using System.Collections.Generic;
using System.IO;
using Hololens.Assets.Scripts.Connection.Model;
using UnityEngine;

namespace Hololens.Assets.Scripts.Connection.Utils
{
    public static class FileProcessor
    {
        public static byte[] ReadFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File was not found: {filePath}");
            }

            return File.ReadAllBytes(filePath);
        }

        public static IEnumerable<Packet> SplitFile(string channelName, string filePath)
        {
            byte[] data = ReadFile(filePath);
            int totalChunks = (int)Math.Ceiling((double)data.Length / AppConfig.CHUNK_BUFFER_SIZE);
            List<Packet> packets = new();

            int processedBytes = 0;

            for (int i = 0; processedBytes < data.Length; i++)
            {
                int remainingBytes = data.Length - processedBytes;
                int size = Math.Min(AppConfig.CHUNK_BUFFER_SIZE, remainingBytes);

                int adjustedSize = size;

                // Look for the nearest newline ('\n') before the end of this chunk
                while (adjustedSize > 0 && data[processedBytes + adjustedSize - 1] != '\n')
                {
                    adjustedSize--;
                }

                if (adjustedSize == 0)
                {
                    adjustedSize = size;
                }

                byte[] chunkData = new byte[adjustedSize];
                Buffer.BlockCopy(data, processedBytes, chunkData, 0, adjustedSize);

                var packet = new Packet(
                    CompactGenerator.GeneratePacketId(),
                    (Channel)Enum.Parse(typeof(Channel), channelName, true),
                    new Chunk(i, totalChunks, chunkData)
                );

                packets.Add(packet);
                processedBytes += adjustedSize;
            }

            return packets;
        }

        public static void ReassembleFile(string channelName, List<Packet> packets)
        {
            string outputPath = Path.Combine(
                Environment.CurrentDirectory,
                $"{channelName}_{DateTime.UtcNow:yyyyMMddHHmmss}.obj"
            );

            packets.Sort((a, b) => a.Chunk.SequenceNumber.CompareTo(b.Chunk.SequenceNumber));

            using (var fileStream = new FileStream(outputPath, FileMode.Create))
            {
                foreach (var packet in packets)
                {
                    fileStream.Write(packet.Chunk.Data, 0, packet.Chunk.Data.Length);
                }
            }

            Debug.Log($"Reassembled file saved at: {outputPath}");
        }
    }
}
