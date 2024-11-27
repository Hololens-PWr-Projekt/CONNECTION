using System;
using System.Collections.Generic;
using Server.ViewModel;
using Server.Model;

namespace Server.Service
{
    public class DataProcessor
    {
        private readonly MainWindowViewModel _viewModel;
        private readonly Dictionary<string, List<Chunk>> _chunksBuffer;
        private readonly Dictionary<string, Action<string>> _dataHandlers;

        public DataProcessor(MainWindowViewModel viewModel)
        {
            // Define packetType data handlers
            _dataHandlers = new Dictionary<string, Action<string>>
            {
                { "vertices", ProcessVerticesData },
                { "triangles", ProcessTrianglesData }
            };

            _viewModel = viewModel;
            _chunksBuffer = [];
        }

        public void ProcessPacket(string message)
        {
            var packet = JsonManager.Deserialize<Packet>(message);

            if (packet == null)
            {
                Console.WriteLine("Invalid packet received!");
                return;
            }

            if (!_chunksBuffer.ContainsKey(packet.PacketId))
            {
                _chunksBuffer[packet.PacketId] = [];
            }

            _chunksBuffer[packet.PacketId].Add(packet.Chunk);

            // Reassemble if all packets have been received
            if (packet.Chunk.SequenceNumber == packet.Chunk.TotalChunks)
            {
                ReassemblePacket(packet.PacketId, packet.PacketType);
            }
        }

        private void ReassemblePacket(string packetId, string packetType)
        {
            if (!_chunksBuffer.TryGetValue(packetId, out var chunks))
            {
                Console.WriteLine($"No data found for packet ID: {packetId}");
                return;
            }

            // Sort by sequence number and merge chunks data
            chunks.Sort((a, b) => a.SequenceNumber.CompareTo(b.SequenceNumber));
            var mergedData = string.Join("", chunks.ConvertAll(chunk => chunk.Data));
            // Get rid of escaped double quotes
            string convertedData = JsonManager.Deserialize<string>(mergedData);

            if (_dataHandlers.TryGetValue(packetType, out var handler))
            {
                handler(convertedData);
            }
            else
            {
                Console.WriteLine($"Unknown packet type: {packetType}");
            }

            _chunksBuffer.Remove(packetId);
        }

        private void ProcessTrianglesData(string data)
        {
            var triangles = JsonManager.Deserialize<List<int>>(data);
            _viewModel.Triangles = triangles;
        }

        private void ProcessVerticesData(string data)
        {
            var vertices = JsonManager.Deserialize<List<Vertex>>(data);
            _viewModel.Vertices = vertices;
        }
    }
}