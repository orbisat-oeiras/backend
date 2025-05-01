// TODO: Resync packets that come in different timestamps
using System.Security.Cryptography.X509Certificates;
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
            ulong currentTimestamp = (ulong)(
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000
            );
            const ulong STALE_THRESHOLD_NANOSECONDS = 1_500_000_000; // 1.5 seconds
            if (currentTimestamp - packet.Timestamp > STALE_THRESHOLD_NANOSECONDS)
            {
                return;
            }

            _resyncBuffer.Add(packet);
        }

        public List<Packet> GetNextGroup()
        {
            if (_resyncBuffer.Count == 0)
                return [];

            _resyncBuffer.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
            ulong firstTimestamp = _resyncBuffer[0].Timestamp;

            List<Packet> group =
            [
                .. _resyncBuffer.TakeWhile(p => p.Timestamp - firstTimestamp <= WINDOW_NANOSECONDS),
            ];

            foreach (Packet packet in group)
                _resyncBuffer.Remove(packet);

            return group;
        }
    }
}
