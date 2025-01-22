using System;
using System.Collections.Generic;
using System.Linq;
using Hololens.Assets.Scripts.Connection.Model;
using UnityEngine;
#if WINDOWS_UWP
using Windows.Storage;
using Windows.Storage.Streams;
using System.Threading.Tasks;
#endif

namespace Hololens.Assets.Scripts.Connection.Utils
{
    public static class FileProcessor
    {
        public static byte[] ReadFile(string filePath)
        {
#if WINDOWS_UWP
            return ReadFileUWP(filePath).Result;
#else
            if (!System.IO.File.Exists(filePath))
            {
                throw new System.IO.FileNotFoundException($"File was not found: {filePath}");
            }

            return System.IO.File.ReadAllBytes(filePath);
#endif
        }

#if WINDOWS_UWP
        private static async Task<byte[]> ReadFileUWP(string filePath)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);
            using (IRandomAccessStream stream = await file.OpenReadAsync())
            {
                byte[] data = new byte[stream.Size];
                using (DataReader reader = new DataReader(stream))
                {
                    await reader.LoadAsync((uint)stream.Size);
                    reader.ReadBytes(data);
                }
                return data;
            }
        }
#endif

        public static IEnumerable<Packet> SplitFile(string channelName, byte[] data)
        {
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
#if WINDOWS_UWP
            ReassembleFileUWP(channelName, packets).Wait();
#else
            string outputPath = System.IO.Path.Combine(
                Environment.CurrentDirectory,
                $"{channelName}_{DateTime.UtcNow:yyyyMMddHHmmss}.obj"
            );

            packets.Sort((a, b) => a.Chunk.SequenceNumber.CompareTo(b.Chunk.SequenceNumber));

            using (var fileStream = new System.IO.FileStream(outputPath, System.IO.FileMode.Create))
            {
                foreach (var packet in packets)
                {
                    fileStream.Write(packet.Chunk.Data, 0, packet.Chunk.Data.Length);
                }
            }

            Debug.Log($"Reassembled file saved at: {outputPath}");
#endif
        }

#if WINDOWS_UWP
        private static async Task ReassembleFileUWP(string channelName, List<Packet> packets)
        {
            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
            string fileName = $"{channelName}_{DateTime.UtcNow:yyyyMMddHHmmss}.obj";
            StorageFile outputFile = await storageFolder.CreateFileAsync(
                fileName,
                CreationCollisionOption.ReplaceExisting
            );

            packets.Sort((a, b) => a.Chunk.SequenceNumber.CompareTo(b.Chunk.SequenceNumber));

            using (
                IRandomAccessStream stream = await outputFile.OpenAsync(FileAccessMode.ReadWrite)
            )
            {
                using (DataWriter writer = new DataWriter(stream))
                {
                    foreach (var packet in packets)
                    {
                        writer.WriteBytes(packet.Chunk.Data);
                    }

                    await writer.StoreAsync();
                    await writer.FlushAsync();
                }
            }

            Debug.Log($"Reassembled file saved at: {outputFile.Path}");
        }
#endif
    }
}
