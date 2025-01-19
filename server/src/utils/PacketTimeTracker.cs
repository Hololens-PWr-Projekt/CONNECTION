using System;
using System.Collections.Generic;
using System.Linq;
using Server.Src.Model;

namespace Server.Src.Utils
{
    public class PacketTimeTracker
    {
        private List<TimeSpan> _timeSpans;

        public PacketTimeTracker()
        {
            _timeSpans = new List<TimeSpan>();
        }

        public void PrintPacketTransferTime(Packet packet)
        {
            TimeSpan timeDiff = DateTime.UtcNow - packet.Timestamp;
            // Console.WriteLine(
            //     $"Packet {packet.PacketId} ({packet.Chunk.SequenceNumber + 1}/{packet.Chunk.TotalChunks}) received after {timeDiff.TotalMilliseconds} ms"
            // );
            _timeSpans.Add(timeDiff);
        }

        public void PrintAveragePacketTransferTime()
        {
            if (_timeSpans.Count == 0)
            {
                Console.WriteLine("No packets received yet.");
                return;
            }

            TimeSpan averageTime = TimeSpan.FromMilliseconds(
                _timeSpans.Average(ts => ts.TotalMilliseconds)
            );

            _timeSpans.Clear();

            Console.WriteLine($"Average packet transfer time: {averageTime.TotalMilliseconds} ms");
        }
    }
}
