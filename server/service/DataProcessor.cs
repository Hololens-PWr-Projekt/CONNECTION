using System;
using System.Collections.Generic;
using Server.ViewModel;
using Server.Model;

namespace Server.Service
{
    public class DataProcessor
    {
        private readonly MainWindowViewModel viewModel;
        private readonly Dictionary<string, List<Chunk>> chunksBuffer;
        private readonly Dictionary<string, Action<string>> dataHandlers;

        public DataProcessor(MainWindowViewModel viewModel)
        {
            // Define data handlers for each packet type
            dataHandlers = new Dictionary<string, Action<string>>
            {
                { "vertices", ProcessVerticesData },
                { "triangles", ProcessTrianglesData }
            };

            this.viewModel = viewModel;
            chunksBuffer = [];
        }

        public void ProcessPacket(string message)
        {
            var packet = JsonManager.Deserialize<Packet>(message);

            if (packet == null)
            {
                Console.WriteLine("Invalid packet received!");
                return;
            }

            if (!chunksBuffer.TryGetValue(packet.PacketId, out var _))
            {
                chunksBuffer[packet.PacketId] = [];
            }

            chunksBuffer[packet.PacketId].Add(packet.Chunk);

            // Reassemble if all packets have been received
            if (packet.Chunk.SequenceNumber == packet.Chunk.TotalChunks)
            {
                ReassemblePacket(packet.PacketId, packet.PacketType);
            }
        }

        private void ReassemblePacket(string packetId, string packetType)
        {
            if (!chunksBuffer.TryGetValue(packetId, out var chunks))
            {
                Console.WriteLine($"No data found for packet ID: {packetId}");
                return;
            }

            // Sort by sequence number and merge chunks data
            chunks.Sort((a, b) => a.SequenceNumber.CompareTo(b.SequenceNumber));
            var mergedData = string.Join("", chunks.ConvertAll(chunk => chunk.Data));

            // Get rid of escaped double quotes
            var convertedData = JsonManager.Deserialize<string>(mergedData);

            if (dataHandlers.TryGetValue(packetType, out var handler))
            {
                handler(convertedData);
            }
            else
            {
                Console.WriteLine($"Unknown packet type: {packetType}");
            }

            chunksBuffer.Remove(packetId);
        }

        private void ProcessTrianglesData(string data)
        {
            var triangles = JsonManager.Deserialize<List<int>>(data);
            viewModel.Triangles = triangles;
        }

        private void ProcessVerticesData(string data)
        {
            var vertices = JsonManager.Deserialize<List<Vertex>>(data);
            viewModel.Vertices = vertices;
        }
    }
}
