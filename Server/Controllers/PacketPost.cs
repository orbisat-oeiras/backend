using System.Text;
using backend.Library.Extensions;
using backend.Library.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
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

        public PacketPost(ILogger<PacketPost> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        [HttpPost]
        public ActionResult<Orbipacket.Packet> PostPacket(Orbipacket.Packet packet)
        {
            ISerialSender? _serialSender = _serviceProvider.GetKeyedService<ISerialSender>(
                ServiceKeys.SerialSender
            );
            _logger.LogInformation(
                "Received packet with ID: {packetId} at {time} with data {data}, as type {type}",
                packet.DeviceId,
                packet.Timestamp,
                Encoding.ASCII.GetString(packet.Payload.Value),
                packet.Type
            );

            if (_serialSender == null)
            {
                _logger.LogError("ISerialSender service is not available.");
                return StatusCode(500, "ISerialSender service is not available.");
            }
            byte[] encodedData = Encode.EncodePacket(packet);
            _serialSender.SendPacket(encodedData);
            _logger.LogInformation(
                "Encoded packet data: {encodedData}",
                BitConverter.ToString(encodedData)
            );
            return Ok(packet);
        }
    }
}
