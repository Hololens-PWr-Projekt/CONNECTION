using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Hololens.Assets.Scripts.Connection.Model;
using UnityEngine;
#if ENABLE_WINMD_SUPPORT
using Windows.Storage;
using Windows.Storage.Streams;
#endif

namespace Hololens.Assets.Scripts.Connection.Utils
{
    public static class FileProcessor
    {
#if ENABLE_WINMD_SUPPORT
        public static async Task<byte[]> ReadFileAsync(string filePath)
        {
            try
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);
                using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read))
                {
                    var buffer = new byte[stream.Size];
                    using (DataReader reader = new DataReader(stream.GetInputStreamAt(0)))
                    {
                        await reader.LoadAsync((uint)stream.Size);
                        reader.ReadBytes(buffer);
                        return buffer;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new FileNotFoundException($"Failed to read file: {filePath}", ex);
            }
        }

        public static async Task<IEnumerable<Packet>> SplitFileAsync(
            string channelName,
            byte[] data
        )
        {
            return await Task.Run(() =>
            {
                int totalChunks = (int)
                    Math.Ceiling((double)data.Length / AppConfig.CHUNK_BUFFER_SIZE);
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
                    System.Buffer.BlockCopy(data, processedBytes, chunkData, 0, adjustedSize);

                    var packet = new Packet(
                        CompactGenerator.GeneratePacketId(),
                        (Channel)Enum.Parse(typeof(Channel), channelName, true),
                        new Chunk(i, totalChunks, chunkData)
                    );

                    packets.Add(packet);
                    processedBytes += adjustedSize;
                }

                return packets;
            });
        }

        public static async Task ReassembleFileAsync(string channelName, List<Packet> packets)
        {
            string outputFileName = $"{channelName}_{DateTime.UtcNow:yyyyMMddHHmmss}.obj";
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile file = await folder.CreateFileAsync(
                outputFileName,
                CreationCollisionOption.ReplaceExisting
            );

            packets.Sort((a, b) => a.Chunk.SequenceNumber.CompareTo(b.Chunk.SequenceNumber));

            using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                using (DataWriter writer = new DataWriter(stream))
                {
                    foreach (var packet in packets)
                    {
                        writer.WriteBytes(packet.Chunk.Data);
                    }
                    await writer.StoreAsync();
                }
            }

            Debug.Log($"Reassembled file saved at: {file.Path}");
        }
#else
        public static byte[] ReadFile(string filePath)
        {
            Debug.LogWarning("ReadFileAsync is not supported in this build configuration.");
            return Array.Empty<byte>();
        }

        public static Task<IEnumerable<Packet>> SplitFileAsync(string channelName, byte[] data)
        {
            Debug.LogWarning("SplitFileAsync is not supported in this build configuration.");
            return Task.FromResult<IEnumerable<Packet>>(new List<Packet>());
        }

        public static Task ReassembleFileAsync(string channelName, List<Packet> packets)
        {
            Debug.LogWarning("ReassembleFileAsync is not supported in this build configuration.");
            return Task.CompletedTask;
        }
#endif
    }
}
