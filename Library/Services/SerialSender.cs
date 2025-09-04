using System.IO.Ports;
using Microsoft.Extensions.Logging;
using Orbipacket;

namespace backend.Library.Services
{
    public class SerialSender : ISerialSender, IDisposable
    {
        private readonly ILogger<SerialSender> _logger;
        private readonly SerialPort _serialport;

        public SerialSender(
            string portName,
            int baudRate,
            Parity parity,
            ILogger<SerialSender> logger
        )
        {
            _logger = logger;
            _serialport = new SerialPort(portName, baudRate, parity)
            {
                ReadTimeout = 400,
                WriteTimeout = 400,
            };
            try
            {
                _serialport.Open();
                _logger.LogInformation(
                    "Opened serial port {portName} at {baudRate} baud.",
                    portName,
                    baudRate
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open serial port {portName}.", portName);
                throw;
            }
        }

        public void SendPacket(byte[] packetData)
        {
            if (!_serialport.IsOpen)
            {
                _logger.LogWarning("Serial port is not open. Cannot send packet.");
                return;
            }

            try
            {
                _serialport.Write(packetData, 0, packetData.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send packet over serial port.");
            }
        }

        public void Dispose()
        {
            if (_serialport.IsOpen)
            {
                _serialport.Close();
                _logger.LogInformation("Closed serial port.");
            }
            _serialport.Dispose();
        }
    }

    /// <summary>
    /// Interface for sending packets over a serial connection
    /// </summary>
    public interface ISerialSender
    {
        /// <summary>
        /// Sends a packet over the selected serial port.
        /// </summary>
        /// <param name="packetData">The encoded packet data</param>
        void SendPacket(byte[] packetData);
    }
}
