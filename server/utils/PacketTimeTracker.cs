using System;
using Server.Model;

namespace Server.Utils
{
    public static class PacketTimeTracker
    {
        public static void CalculateAndPrintTimeDiff(Packet packet)
        {
            var timeDiff = DateTime.UtcNow - packet.Timestamp;
            Console.WriteLine(
                $"Packet {packet.PacketId} ({packet.Chunk.SequenceNumber}/{packet.Chunk.TotalChunks}) received after {timeDiff.TotalMilliseconds} ms"
            );
        }
    }
}
