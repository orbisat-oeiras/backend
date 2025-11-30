using Orbipacket;

namespace backend.Library.Services.DataProcessors
{
    internal class PacketResync
    {
        private const ulong WINDOW_NANOSECONDS = 100_000_000; // 100ms window
        private const ulong STALE_THRESHOLD_NANOSECONDS = 1_000_000_000; // 1.0 seconds
        private readonly List<Packet> _resyncBuffer = [];

        private ulong? _currentTimestamp = null;

        public void AddPacket(Packet packet)
        {
            if (
                _currentTimestamp != null
                && packet.Timestamp - _currentTimestamp > STALE_THRESHOLD_NANOSECONDS
            )
            {
                // Console.WriteLine(
                //     "Difference between packet times:" + (packet.Timestamp - currentTimestamp)
                // );
                // Console.WriteLine("Current Timestamp:" + currentTimestamp);
                // Console.WriteLine("Packet Timestamp:" + packet.Timestamp);
                _currentTimestamp = packet.Timestamp;
                return;
            }
            _resyncBuffer.Add(packet);

            _currentTimestamp = packet.Timestamp;
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

            _currentTimestamp = null;
            return group;
        }
    }
}
