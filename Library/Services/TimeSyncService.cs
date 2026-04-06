using System.Text;
using backend.Library.Models;
using backend.Library.Services.DataProcessors.DataExtractors;
using backend.Library.Services.DataProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orbipacket;
using Orbipacket.Library;

namespace backend.Library.Services
{
    public class TimeSyncService
    {
        private readonly ILogger<TimeSyncService> _logger;
        private ulong t0;
        private ulong t1,
            t2,
            t3 = 0;
        private ulong t0Check;
        private long _offset = 0;
        public long Offset => _offset;

        private readonly IPacketSender _serialSender;

        /// <summary>
        /// Create a new instance of TimeSyncService
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="serialSender"></param>
        /// <param name="dataProvider"></param>
        public TimeSyncService(
            ILogger<TimeSyncService> logger,
            [FromKeyedServices(ServiceKeys.SerialSender)] IPacketSender serialSender,
            [FromKeyedServices(ServiceKeys.DataProvider)]
                IDataProvider<Dictionary<SerialProvider.DataLabel, byte[]>> dataProvider
        )
        {
            _logger = logger;
            _serialSender = serialSender;
            dataProvider.OnDataProvided += HandleIncomingData;
        }

        public void BeginSync()
        {
            _logger.LogInformation("Beginning sync...");

            t0 = (ulong)((DateTime.UtcNow.Ticks - DateTime.UnixEpoch.Ticks) / 10);
            // This comes in 100-nanosecond interval, div by 10 is microseconds
            // also for some weird ahh reason utcnow is the time since 00:00:00 01-01-0000 instead of epoch
            // that's why we subtract datetime.unixepoch.ticks (which is just the amount of 100-nanosecond interval from utcnow to unixepoch,
            // it's a constant variable)

            Packet syncRequest = new(
                DeviceId.TimeSync,
                0,
                new Payload(BitConverter.GetBytes(t0)),
                Packet.PacketType.TcPacket
            );

            byte[] packet = Encode.EncodePacket(syncRequest);

            _serialSender.SendPacket(packet);
            _logger.LogInformation("Packet Sent!");
        }

        private void HandleIncomingData(
            EventData<Dictionary<SerialProvider.DataLabel, byte[]>> incomingData
        )
        {
            foreach (KeyValuePair<SerialProvider.DataLabel, byte[]> keyValue in incomingData.Data)
            {
                if (keyValue.Key == SerialProvider.DataLabel.TimeSync)
                {
                    _logger.LogInformation("Calculating offset...");

                    t3 = (ulong)((DateTime.UtcNow.Ticks - DateTime.UnixEpoch.Ticks) / 10);
                    _logger.LogInformation(BitConverter.ToString(keyValue.Value));
                    t0Check = BitConverter.ToUInt64(keyValue.Value, 0);
                    if (t0Check != t0)
                    {
                        _logger.LogInformation("Error: expected t0: " + t0 + " got: " + t0Check);
                        return;
                    }
                    t1 = BitConverter.ToUInt64(keyValue.Value, 8);
                    t2 = BitConverter.ToUInt64(keyValue.Value, 16);

                    _offset = ((long)t1 - (long)t0 + ((long)t2 - (long)t3)) / 2;

                    _logger.LogInformation(
                        $"Clock offset calculated: {_offset}, with CanSat t1 = {t1} and t2 = {t2} and local t3 = {t3}"
                    );
                }
            }
        }
    }
}
