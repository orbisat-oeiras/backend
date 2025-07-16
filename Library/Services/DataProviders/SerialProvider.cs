using System.Globalization;
using System.IO.Ports;
using System.Text;
using backend.Library.Models;
using backend.Library.Services.DataProcessors;
using Microsoft.Extensions.Logging;
using Orbipacket;

namespace backend.Library.Services.DataProviders
{
    /// <summary>
    /// Provides data read from a serial port.
    /// </summary>
    public sealed class SerialProvider
        : IDataProvider<Dictionary<SerialProvider.DataLabel, byte[]>>,
            IDisposable
    {
        /// <summary>
        /// Represent the index of each data piece in the list provided by a SerialProvider.
        /// </summary>
        public enum DataLabel
        {
            System,
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
            GPSData,
            AccelerationData,
            Unknown,
        }

        public event Action<EventData<Dictionary<DataLabel, byte[]>>>? OnDataProvided;

        // Logger provided by DI, used for printing information to all logging providers at once
        private readonly ILogger<SerialProvider> _logger;
        private readonly SerialPort _serialPort;
        private readonly PacketResync packetResync = new();
        private readonly object _lock = new();
        private bool _isProcessing;
        private readonly PacketBuffer _packetBuffer = new();
        private readonly Dictionary<DataLabel, byte[]> _currentData;

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
                ReadTimeout = 400,
                WriteTimeout = 400,
            };
            // Open the port
            _serialPort.Open();

            // Set up event listeners
            _timer = new System.Timers.Timer(200) { AutoReset = true };
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
            lock (_lock)
            {
                _timer.Start();
                try
                {
                    if (_isProcessing)
                    {
                        return;
                    }

                    _isProcessing = true;
                    _timer.Stop();
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
                        _logger.LogInformation(
                            "Extracted packet: {packet}",
                            BitConverter.ToString([.. extractedPacket.Skip(2)]).Replace("-", "")
                        );
                        Packet? packet = Decode.GetPacketInformation(extractedPacket);

                        if (packet == null || packet.Payload?.Value == null)
                        {
                            _logger.LogWarning("Invalid or corrupted packet.");
                        }
                        else
                        {
                            packetResync.AddPacket(packet);
                            newDataArrived = true;
                        }
                    }
                    if (!newDataArrived)
                    {
                        _logger.LogWarning("No valid packets extracted from buffer.");
                    }
                    if (newDataArrived)
                    {
                        List<Packet>? list;
                        _logger.LogInformation("Getting next group of packets...");
                        list = packetResync.GetNextGroup();

                        if (list == null)
                        {
                            _logger.LogWarning("No packets returned by GetNextGroup.");
                            return;
                        }

                        foreach (Packet packet in list)
                        {
                            DataLabel label = packet.DeviceId switch
                            {
                                DeviceId.PressureSensor => DataLabel.Pressure,
                                DeviceId.TemperatureSensor => DataLabel.Temperature,
                                DeviceId.HumiditySensor => DataLabel.Humidity,
                                DeviceId.System => DataLabel.System,
                                DeviceId.Unknown => DataLabel.Unknown,
                                DeviceId.GPS => DataLabel.GPSData,
                                DeviceId.Accelerometer => DataLabel.AccelerationData,
                                _ => throw new NotImplementedException(),
                            };
                            _currentData[label] = packet.Payload.Value;

                            // These logs can easily be removed, but
                            // they are converting from byte[] to string at every packet received.
                            if (label == DataLabel.System)
                            {
                                _logger.LogInformation(
                                    "System data: {data}",
                                    Encoding.ASCII.GetString(packet.Payload.Value)
                                );
                            }
                            else
                            {
                                _logger.LogInformation(
                                    "{label} data: {data}",
                                    label,
                                    BitConverter
                                        .ToSingle(packet.Payload.Value, 0)
                                        .ToString(CultureInfo.InvariantCulture)
                                );
                            }
                        }

                        Dictionary<DataLabel, byte[]> dict = new(_currentData);
                        ulong timestamp = list[0].Timestamp;

                        GPSCoords coords = new()
                        {
                            Latitude = _currentData.TryGetValue(
                                DataLabel.GPSData,
                                out byte[]? latBytes
                            )
                                ? BitConverter.ToDouble(latBytes, 0)
                                : double.NaN,
                            Longitude = _currentData.TryGetValue(
                                DataLabel.GPSData,
                                out byte[]? lonBytes
                            )
                                ? BitConverter.ToDouble(lonBytes, 8)
                                : double.NaN,
                        };

                        _logger.LogInformation(
                            "GPS Data: {coords}",
                            coords.Latitude + ", " + coords.Longitude
                        );

                        OnDataProvided?.Invoke(
                            new EventData<Dictionary<DataLabel, byte[]>>
                            {
                                DataStamp = new DataStamp
                                {
                                    Timestamp = timestamp,
                                    Coordinates = coords,
                                },
                                Data = dict,
                            }
                        );
                        _currentData.Clear();
                    }
                }
                finally
                {
                    _isProcessing = false;
                    _timer.Start();
                }
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
