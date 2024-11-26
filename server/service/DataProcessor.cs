using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Server.ViewModel;
using Server.Model;

namespace Server.Service
{
    public class DataProcessor(MainWindowViewModel viewModel)
    {
        private readonly MainWindowViewModel _viewModel = viewModel;
        private readonly Dictionary<string, List<Chunk>> _packetBuffer = [];

        public void ProcessPacket(string message)
        {
            var packet = JsonConvert.DeserializeObject<Packet>(message);

            if (packet == null)
            {
                Console.WriteLine("Invalid packet received!");
                return;
            }

            if (!_packetBuffer.ContainsKey(packet.PacketId))
            {
                _packetBuffer[packet.PacketId] = [];
            }

            _packetBuffer[packet.PacketId].Add(packet.Chunk);

            if (packet.Chunk.SequenceNumber == packet.Chunk.TotalChunks)
            {
                ReassemblePacket(packet.PacketId, packet.PacketType);
            }
        }

        private void ReassemblePacket(string packetId, string packetType)
        {
            if (!_packetBuffer.TryGetValue(packetId, out var chunks))
            {
                Console.WriteLine($"No data found for packet ID: {packetId}");
                return;
            }

            chunks.Sort((a, b) => a.SequenceNumber.CompareTo(b.SequenceNumber));
            // Combine data from all chunks
            var fullData = string.Join("", chunks.ConvertAll(chunk => chunk.Data));
            switch (packetType)

            {
                case "vertices":
                    ProcessVerticesData(fullData);
                    break;

                case "triangles":
                    ProcessTrinaglesData(fullData);
                    break;
            }

            _packetBuffer.Remove(packetId);
        }

        private void ProcessTrinaglesData(string data)
        {
            try
            {
                var triangles = JsonConvert.DeserializeObject<List<int>>(data);
                Console.WriteLine(triangles);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error processing triangles data: {e.Message}");
            }
        }

        private void ProcessVerticesData(string data)
        {
            try
            {
                var vertices = JsonConvert.DeserializeObject<List<Vertex>>(data);
                Console.WriteLine(vertices);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error processing vertices data: {e.Message}");
            }
        }
    }
}