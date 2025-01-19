using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Server.Src.Manager;
using Server.Src.Model;
using Server.Src.Utils;

namespace Server.Manager
{
    public class ChannelManager
    {
        private readonly ConcurrentDictionary<string, ChannelState> _channels = new();

        public void AddChannel(string channelName, WebSocketManager manager)
        {
            if (_channels.ContainsKey(channelName))
            {
                Console.WriteLine($"Channel {channelName} already exists.");
                return;
            }

            var state = new ChannelState(manager);
            _channels[channelName] = state;

            Task.Run(
                () => ProcessIncomingPackets(channelName, state.CancellationTokenSource.Token)
            );
            Console.WriteLine($"Channel {channelName} added and ready to process packets.");
        }

        public void RemoveChannel(string channelName)
        {
            if (_channels.TryRemove(channelName, out var state))
            {
                state.CancellationTokenSource.Cancel();
                Task.Run(() => state.Manager.CloseAsync());
                Console.WriteLine($"Channel {channelName} removed.");
            }
        }

        public void EnqueuePacket(string channelName, Packet packet)
        {
            if (!_channels.TryGetValue(channelName, out var state))
            {
                Console.WriteLine($"Channel {channelName} is not registered.");
                return;
            }

            state.PacketQueue.Enqueue(packet);
        }

        private async Task ProcessIncomingPackets(
            string channelName,
            CancellationToken cancellationToken
        )
        {
            if (!_channels.TryGetValue(channelName, out var state))
            {
                Console.WriteLine($"Channel {channelName} is not registered.");
                return;
            }

            List<Packet> packets = new();

            while (!cancellationToken.IsCancellationRequested)
            {
                if (state.PacketQueue.TryDequeue(out var packet))
                {
                    if (packet.PacketId == "start")
                    {
                        Console.WriteLine($"Start signal received for channel {channelName}.");
                        packets.Clear();
                    }
                    else if (packet.PacketId == "stop")
                    {
                        Console.WriteLine($"Stop signal received for channel {channelName}.");

                        string reassembledFilePath = await ReassembleFile(channelName, packets);

                        await SendReassembledFileAsPacketsAsync(channelName, reassembledFilePath);
                    }
                    else
                    {
                        packets.Add(packet);
                    }
                }
                else
                {
                    await Task.Delay(50, cancellationToken);
                }
            }
        }

        private static async Task<string> ReassembleFile(string channelName, List<Packet> packets)
        {
            List<string> mergedVertices = new List<string>();
            List<string> mergedFaces = new List<string>();
            int vertexOffset = 0;

            string outputPath = Path.Combine(
                Environment.CurrentDirectory,
                $"{channelName}_{DateTime.UtcNow:yyyyMMddHHmmss}.obj"
            );

            packets.Sort((a, b) => a.Chunk.SequenceNumber.CompareTo(b.Chunk.SequenceNumber));

            foreach (var packet in packets)
            {
                byte[] data = packet.Chunk.Data;
                string message = Encoding.UTF8.GetString(data, 0, data.Length);
                string[] lines = message.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    if (line.StartsWith("v "))
                    {
                        mergedVertices.Add(line);
                    }
                    else if (line.StartsWith("f "))
                    {
                        string[] parts = line.Split(' ');
                        List<string> adjustedFace = new List<string> { "f" };

                        for (int i = 1; i < parts.Length; i++)
                        {
                            if (parts[i].Contains('/'))
                            {
                                string[] subParts = parts[i].Split('/');
                                int vertexIndex = int.Parse(subParts[0]) + vertexOffset;
                                string adjustedFacePart = vertexIndex.ToString();

                                if (subParts.Length > 1)
                                {
                                    adjustedFacePart += "/" + subParts[1];
                                }

                                if (subParts.Length > 2)
                                {
                                    adjustedFacePart += "/" + subParts[2];
                                }

                                adjustedFace.Add(adjustedFacePart);
                            }
                            else
                            {
                                int vertexIndex = int.Parse(parts[i]) + vertexOffset;
                                adjustedFace.Add(vertexIndex.ToString());
                            }
                        }

                        mergedFaces.Add(string.Join(" ", adjustedFace));
                    }

                    vertexOffset = mergedVertices.Count;
                }

                using StreamWriter writer = new(outputPath);
                await writer.WriteLineAsync("# Merged OBJ File");

                foreach (var vertex in mergedVertices)
                {
                    await writer.WriteLineAsync(vertex);
                }

                foreach (var face in mergedFaces)
                {
                    await writer.WriteLineAsync(face);
                }
            }

            Console.WriteLine($"Reassembled file saved at: {outputPath}");
            return outputPath;
        }

        private async Task SendReassembledFileAsPacketsAsync(string channelName, string filePath)
        {
            if (!_channels.TryGetValue(channelName, out var state))
            {
                throw new InvalidOperationException($"Channel {channelName} not found.");
            }

            // Read the reassembled file into a byte array
            var data = await File.ReadAllBytesAsync(filePath);
            int totalChunks = (int)Math.Ceiling((double)data.Length / AppConfig.CHUNK_BUFFER_SIZE);

            for (int i = 0; i < totalChunks; i++)
            {
                int offset = i * AppConfig.CHUNK_BUFFER_SIZE;
                int size = Math.Min(AppConfig.CHUNK_BUFFER_SIZE, data.Length - offset);

                byte[] chunkData = new byte[size];
                Buffer.BlockCopy(data, offset, chunkData, 0, size);

                var packet = new Packet(
                    CompactGenerator.GeneratePacketId(),
                    (Channel)Enum.Parse(typeof(Channel), channelName, true),
                    new Chunk(i, totalChunks, chunkData)
                );

                // Send the packet
                await state.Manager.SendAsync(packet);
                Console.WriteLine($"Packet {i + 1}/{totalChunks} sent to channel {channelName}.");
            }

            Console.WriteLine($"All packets for channel {channelName} have been sent.");
        }
    }

    internal class ChannelState
    {
        public WebSocketManager Manager { get; }
        public ConcurrentQueue<Packet> PacketQueue { get; }
        public CancellationTokenSource CancellationTokenSource { get; }

        public ChannelState(WebSocketManager manager)
        {
            Manager = manager;
            PacketQueue = new ConcurrentQueue<Packet>();

            CancellationTokenSource = new CancellationTokenSource();
        }
    }
}
