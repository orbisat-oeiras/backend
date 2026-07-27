using System.Collections.Generic;
using System.Linq;
using Orbipacket;

namespace backend.Library.Services.DataProcessors
{
    internal class PacketResync
    {
        // For the packets to be considered from the same point in time,
        // they have to have a 400ms time between them.
        // TODO: explain what happens if exceeded
        private const ulong WINDOW_MICROSECONDS = 400_000;

        private readonly List<Packet> _resyncBuffer = [];
        private ulong? _lastSeenTimestamp = null;

        /// <summary>
        /// Adds a packet to the current packet buffer.
        /// </summary>
        /// <param name="packet">The packet to be added.</param>
        /// <returns>True if the packet belongs to the current time batch. Returns false if it starts a new time batch (signaling you should extract the packets from the completed batch).</returns>
        public bool AddPacket(Packet packet)
        {
            if (_lastSeenTimestamp == null)
            {
                _resyncBuffer.Add(packet);
                _lastSeenTimestamp = packet.Timestamp;
                return true;
            }

            // If packet is outside the current window, it belongs to a new batch
            // Don't clear the buffer here, beacuse we will force-read everything already inside the buffer
            // on GetNextGroup bc it returned false
            if (AbsValueOfDiff(packet.Timestamp, (ulong)_lastSeenTimestamp) > WINDOW_MICROSECONDS)
            {
                return false;
            }
            _resyncBuffer.Add(packet);
            _lastSeenTimestamp = packet.Timestamp;
            return true;
        }

        // I took this approach bc it

        /// <summary>
        /// Flushes and returns the packet group for the 400ms time window.
        /// If a new packet was held back and was too new for the current group, it automatically gets added to the buffer to the next window.
        /// </summary>
        /// <param name="pendingPacket"></param>
        /// <returns></returns>
        public List<Packet>? GetNextGroup(Packet? pendingPacket = null)
        {
            if (_resyncBuffer.Count == 0)
            {
                if (pendingPacket != null)
                {
                    _resyncBuffer.Add(pendingPacket);
                    _lastSeenTimestamp = pendingPacket.Timestamp;
                }
                return null;
            }

            // Extract the lowest-timestamp packet per DeviceId
            List<Packet> group =
            [
                .. _resyncBuffer.GroupBy(p => p.DeviceId).Select(g => g.MinBy(p => p.Timestamp)!),
            ];

            // Clear old window
            _resyncBuffer.Clear();

            // Start new window with the pending packet if one triggered this flush
            if (pendingPacket != null)
            {
                _resyncBuffer.Add(pendingPacket);
                _lastSeenTimestamp = pendingPacket.Timestamp;
            }
            else
            {
                _lastSeenTimestamp = null;
            }

            return group;
        }

        private static ulong AbsValueOfDiff(ulong a, ulong b)
        {
            return (a >= b) ? (a - b) : (b - a);
        }
    }
}
