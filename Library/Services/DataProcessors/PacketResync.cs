using System.Collections.Generic;
using System.Linq;
using Orbipacket;

namespace backend.Library.Services.DataProcessors
{
    internal class PacketResync
    {
        private const ulong WINDOW_MICROSECONDS = 400_000;
        private const ulong STALE_THRESHOLD_MICROSECONDS = 1_000_000; // 1.0 seconds

        private readonly List<Packet> _resyncBuffer = [];
        private readonly Dictionary<DeviceId, ulong> _lastSeenTimestamps = new();
        private ulong? _currentTimestamp = null;

        private readonly HashSet<DeviceId> _requiredDevices =
        [
            DeviceId.PressureSensor,
            DeviceId.TemperatureSensor,
            DeviceId.HumiditySensor,
        ];

        public bool AddPacket(Packet packet)
        {
            if (
                _currentTimestamp != null
                && packet.Timestamp - _currentTimestamp > STALE_THRESHOLD_MICROSECONDS
            )
            {
                _currentTimestamp = packet.Timestamp;
                _resyncBuffer.Clear();
                _lastSeenTimestamps.Clear();
            }

            if (_lastSeenTimestamps.TryGetValue(packet.DeviceId, out ulong lastSeenTime))
            {
                if (packet.Timestamp - lastSeenTime < WINDOW_MICROSECONDS)
                {
                    return false;
                }
            }

            _lastSeenTimestamps[packet.DeviceId] = packet.Timestamp;
            _resyncBuffer.Add(packet);
            _currentTimestamp = packet.Timestamp;

            return true;
        }

        // Changed return type to nullable List<Packet>?
        public List<Packet>? GetNextGroup()
        {
            if (_resyncBuffer.Count == 0)
                return null;

            _resyncBuffer.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
            ulong firstTimestamp = _resyncBuffer[0].Timestamp;
            ulong latestTimestamp = _resyncBuffer.Last().Timestamp;

            // 2. Check which devices are currently sitting in the buffer
            var devicesInBuffer = _resyncBuffer.Select(p => p.DeviceId).ToHashSet();

            // 3. Are all the required sensors here?
            bool hasAllRequired = _requiredDevices.IsSubsetOf(devicesInBuffer);

            // 4. Has the 400ms time limit expired?
            bool windowExpired = (latestTimestamp - firstTimestamp) >= WINDOW_MICROSECONDS;

            // 5. THE MAGIC LOGIC: Wait if we aren't done yet!
            if (!hasAllRequired && !windowExpired)
            {
                // Return null to tell SerialProvider to keep waiting!
                return null;
            }

            // If we made it here, either we have a perfect full set,
            // OR the 400ms window expired and we are missing a packet (Dead sensor scenario).
            // Extract whatever we managed to catch.
            List<Packet> group =
            [
                .. _resyncBuffer.TakeWhile(p =>
                    p.Timestamp - firstTimestamp <= WINDOW_MICROSECONDS
                ),
            ];

            foreach (Packet packet in group)
                _resyncBuffer.Remove(packet);

            return group;
        }
    }
}
