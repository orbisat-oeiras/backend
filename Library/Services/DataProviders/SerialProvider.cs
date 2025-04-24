using System;
using System.Globalization;
using System.IO.Ports;
using System.Text.RegularExpressions;
using backend.Library.Models;
using backend.Library.Services.DataProcessors;
using backend.Library.Services.DataProcessors.DataExtractors;
using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.Extensions.Logging;
using Orbipacket;

namespace backend.Library.Services.DataProviders
{
    /// <summary>
    /// Provides data read from a serial port.
    /// </summary>
    public sealed class SerialProvider
        : IDataProvider<Dictionary<SerialProvider.DataLabel, string>>,
            IDisposable
    {
        /// <summary>
        /// Represent the index of each data piece in the list provided by a SerialProvider.
        /// </summary>
        /// <remarks>
        /// This enum must be updated to match the formatting of the data sent by the Arduino.
        /// </remarks>
        public enum DataLabel
        {
            Timestamp,
            Pressure,
            Temperature,
            Humidity,
            AccelerationX,
            AccelerationY,
            AccelerationZ,
            Latitude,
            Longitude,
            Altitude,
            Unknown,
        }

        public event Action<EventData<Dictionary<DataLabel, string>>>? OnDataProvided;

        // Logger provided by DI, used for printing information to all logging providers at once
        private readonly ILogger<SerialProvider> _logger;
        private readonly SerialPort _serialPort;
        private readonly Dictionary<DataLabel, int> _schema = [];
        private readonly PacketResync packetResync = new();

        private readonly PacketBuffer _packetBuffer = new();
        private readonly Dictionary<DataLabel, string> _currentData;

        private readonly System.Timers.Timer _timer;

        /// <summary>
        /// Create a new instance of SerialProvider
        /// </summary>
        /// <param name="portName">Name of the port from which data will be read</param>
        /// <param name="baudRate">Baud rate, in bps, of the serial port</param>
        /// <param name="parity">Parity of the serial port</param>
        /// <param name="logger"></param>
        public SerialProvider(
            string portName,
            int baudRate,
            Parity parity,
            ILogger<SerialProvider> logger
        )
        {
            _logger = logger;
            _currentData = [];
            // Note that more options are available for configuring a SerialPort,
            // namely data bits, stop bits and handshake. I have no idea what those
            // are, and am very likely to ever change them in the radio modules
            // configuration, so it's most probably (read hopefully) fine to keep
            // them at the default value.
            // TODO: research about choosing a good baud rate
            _serialPort = new SerialPort(portName, baudRate, parity)
            {
                // I have no clue what a reasonable value for this is
                ReadTimeout = 500,
                WriteTimeout = 500,
            };
            // Open the port
            _serialPort.Open();
            // Set up event listeners
            _timer = new System.Timers.Timer(500) { AutoReset = true };
            _timer.Elapsed += ReceiveAndSendData;
            _timer.Start();
        }

        /// <summary>
        /// Handle data being received on the serial port
        /// </summary>
        /// <param name="sender">The SerialPort object which raised the event</param>
        /// <param name="e"></param>
        private void ReceiveAndSendData(object? sender, System.Timers.ElapsedEventArgs e)
        {
            _logger.LogInformation("Receiving...");

            int byteNumber = _serialPort.BytesToRead;
            byte[] byteBuffer = new byte[byteNumber];

            if (byteNumber != 0)
            {
                _serialPort.Read(byteBuffer, 0, byteNumber);
                _packetBuffer.Add(byteBuffer);
            }

            bool newDataArrived = false;
            byte[]? extractedPacket;

            // Use the same approach I used in testing
            while ((extractedPacket = _packetBuffer.ExtractFirstValidPacket()) != null)
            {
                Packet packet = Decode.GetPacketInformation(extractedPacket);
                Console.WriteLine(
                    "Extracted a packet: Device: " + packet.DeviceId + " Payload: " + packet.Payload
                );
                packetResync.AddPacket(packet);
                newDataArrived = true;
            }
            if (newDataArrived)
            {
                List<Packet>? list;
                _logger.LogInformation("Getting next group of packets...");
                list = packetResync.GetNextGroup();
                if (list != null)
                {
                    foreach (Packet packet in list)
                    {
                        DataLabel label = packet.DeviceId switch
                        {
                            DeviceId.Pressure => DataLabel.Pressure,
                            DeviceId.Temperature => DataLabel.Temperature,
                            DeviceId.Humidity => DataLabel.Humidity,
                            DeviceId.Unknown => DataLabel.Unknown,
                            _ => throw new NotImplementedException(),
                        };
                        // TODO: Remove this .ToString() and treat every payload as byte arrays
                        _currentData[label] = packet.Payload.ToString();
                    }
                }
                Dictionary<DataLabel, string> dict;
                dict = new(_currentData);
                _currentData.Clear();

                ulong timestamp = list[0].Timestamp;
                GPSCoords coords = new()
                {
                    Latitude = float.NaN,
                    Longitude = float.NaN,
                    Altitude = float.NaN,
                };

                OnDataProvided?.Invoke(
                    new EventData<Dictionary<DataLabel, string>>
                    {
                        DataStamp = new DataStamp { Timestamp = timestamp, Coordinates = coords },
                        Data = dict,
                    }
                );
            }
        }

        public void Dispose()
        {
            // Close the serial port so it can be used by other apps
            _serialPort.Close();
            _timer.Stop();
            _timer.Dispose();
        }
    }
}
