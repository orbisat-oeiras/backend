// TODO: Resync packets that come in different timestamps
using Orbipacket;

namespace backend.Library.Services.DataProcessors
{
    internal class PacketResync
    {
        private const ulong WINDOW_NANOSECONDS = 100_000_000; // 100ms window
        private readonly List<Packet> _resyncBuffer = [];

        private ulong? currentTimestamp = null;

        public void AddPacket(Packet packet)
        {
            const ulong STALE_THRESHOLD_NANOSECONDS = 0_500_000_000; // 0.5 seconds

            if (
                currentTimestamp != null
                && packet.Timestamp - currentTimestamp > STALE_THRESHOLD_NANOSECONDS
            )
            {
                // Console.WriteLine(
                //     "Difference between packet times:" + (packet.Timestamp - currentTimestamp)
                // );
                // Console.WriteLine("Current Timestamp:" + currentTimestamp);
                // Console.WriteLine("Packet Timestamp:" + packet.Timestamp);
                currentTimestamp = packet.Timestamp;
                return;
            }
            _resyncBuffer.Add(packet);

            currentTimestamp = packet.Timestamp;
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
