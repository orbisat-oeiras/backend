using System.Text;
using backend.Library.Services;
using backend.Library.Services.DataProviders;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Orbipacket;
using Orbipacket.Library;

namespace backend.Server.Controllers
{
    [ApiController]
    [Route("api/[action]")]
    [EnableCors]
    public class PacketPost : ControllerBase
    {
        private readonly ILogger<PacketPost> _logger;

        private readonly IServiceProvider _serviceProvider;

        private readonly TimeSyncService _timeSyncService;

        public PacketPost(
            ILogger<PacketPost> logger,
            IServiceProvider serviceProvider,
            TimeSyncService timeSyncService
        )
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _timeSyncService = timeSyncService;
        }

        [HttpPost]
        public ActionResult<Packet> PostPacket(Packet packet)
        {
            IPacketSender? _serialSender = _serviceProvider.GetKeyedService<IPacketSender>(
                ServiceKeys.SerialSender
            );

            if (packet.DeviceId == DeviceId.TimeSync)
            {
                _timeSyncService.BeginSync();
                return Ok("Time synchronization started...");
            }
            else
            {
                Packet packetWithAdjustedTimestamp = new(
                    packet.DeviceId,
                    (ulong)((long)packet.Timestamp + _timeSyncService.Offset),
                    packet.Payload,
                    packet.Type
                );

                if (_serialSender == null)
                {
                    _logger.LogError("ISerialSender service is not available.");
                    return StatusCode(500, "ISerialSender service is not available.");
                }
                byte[] encodedData;
                try
                {
                    encodedData = Encode.EncodePacket(packetWithAdjustedTimestamp);
                }
                catch (Exception)
                {
                    _logger.LogError("Timestamp size overflow, sync CanSat time.");
                    return StatusCode(400, "Timestamp size overflow, sync CanSat time.");
                }
                _serialSender.SendPacket(encodedData);
                // _logger.LogInformation(
                //     "Encoded packet data: {encodedData}",
                //     BitConverter.ToString(encodedData)
                // );
                _logger.LogInformation(
                    "Sent packet with ID: {packetId} with CanSatTimestamp {time} with data {data}, as type {type}",
                    packetWithAdjustedTimestamp.DeviceId,
                    packetWithAdjustedTimestamp.Timestamp,
                    Encoding.ASCII.GetString(packetWithAdjustedTimestamp.Payload.Value),
                    packetWithAdjustedTimestamp.Type
                );
                return Ok(packetWithAdjustedTimestamp);
            }
        }
    }
}
