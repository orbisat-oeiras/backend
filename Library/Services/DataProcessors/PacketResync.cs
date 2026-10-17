using System.Collections.Generic;
using System.Linq;
using Orbipacket;

namespace backend.Library.Services.DataProcessors
{
    internal class PacketResync
    {
        // Packets within the same CanSat transmission burst should arrive within 300ms.
        // This is comfortably below the 500ms cycle interval so packets from consecutive
        // cycles will never be merged into the same group.
        private const ulong WINDOW_MICROSECONDS = 300_000;

        private readonly List<Packet> _resyncBuffer = [];
        private ulong? _batchStartTimestamp = null;

        /// <summary>
        /// Adds a packet to the current packet buffer.
        /// </summary>
        /// <param name="packet">The packet to be added.</param>
        /// <returns>True if the packet belongs to the current time batch. Returns false if it starts a new time batch (signaling you should extract the packets from the completed batch).</returns>
        public bool AddPacket(Packet packet)
        {
            if (_batchStartTimestamp == null)
            {
                _resyncBuffer.Add(packet);
                _batchStartTimestamp = packet.Timestamp;
                return true;
            }

            // Check if packet belongs to this batch (anchored to the first packet's timestamp)
            if (AbsValueOfDiff(packet.Timestamp, (ulong)_batchStartTimestamp) > WINDOW_MICROSECONDS)
            {
                return false;
            }

            _resyncBuffer.Add(packet);
            // Note: _batchStartTimestamp stays fixed to anchor the window for this whole batch.
            return true;
        }

        /// <summary>
        /// Flushes and returns the packet group for the time window.
        /// If a new packet triggered this flush, it seeds the next window.
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
                    _batchStartTimestamp = pendingPacket.Timestamp;
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
                _batchStartTimestamp = pendingPacket.Timestamp;
            }
            else
            {
                _batchStartTimestamp = null;
            }

            return group;
        }

        private static ulong AbsValueOfDiff(ulong a, ulong b)
        {
            return (a >= b) ? (a - b) : (b - a);
        }
    }
}
