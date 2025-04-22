// TODO: Resync packets that come in different timestamps
using System.Text.RegularExpressions;
using System.Timers;
using Microsoft.Extensions.Logging;
using Orbipacket;

namespace backend.Library.Services.DataProcessors
{
    internal class PacketResync
    {
        private const ulong WINDOW_NANOSECONDS = 100_000_000; // 100ms window
        private readonly List<Packet> _resyncBuffer = [];

        public void AddPacket(Packet packet)
        {
            _resyncBuffer.Add(packet);
            _resyncBuffer.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
        }

        public List<Packet>? GetNextGroup()
        {
            if (_resyncBuffer.Count == 0)
                return null;

            ulong firstTimestamp = _resyncBuffer[0].Timestamp;
            List<Packet> group =
            [
                .. _resyncBuffer.TakeWhile(packet =>
                    packet.Timestamp - firstTimestamp <= WINDOW_NANOSECONDS
                ),
            ];

            // Only return a group if it contains more than one packet,
            // or if the oldest packet has been waiting "too long" (e.g., > 2s)
            if (group.Count > 1 || IsOldestPacketStale(group))
            {
                // Remove received packets from the buffer
                foreach (Packet? p in group)
                    _resyncBuffer.Remove(p);
                return group;
            }

            return null;
        }

        private static bool IsOldestPacketStale(List<Packet> group)
        {
            ulong now = (ulong)(
                (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds * 1000000
            ); // Convert to nanoseconds

            return (now - group[0].Timestamp) > 2_000_000_000; // 2 seconds
        }
    }
}
