using System;
using System.Globalization;
using System.IO.Ports;
using System.Text.RegularExpressions;
using backend.Library.Models;
using Microsoft.Extensions.Logging;

namespace backend.Library.Services.DataProviders
{
    /// <summary>
    /// Provides data read from a serial port.
    /// </summary>
    public sealed class LegacySerialProvider
        : IDataProvider<Dictionary<LegacySerialProvider.DataLabel, string>>,
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
            AccelerationX,
            AccelerationY,
            AccelerationZ,
            Latitude,
            Longitude,
            Altitude,
        }

        public event Action<EventData<Dictionary<DataLabel, string>>>? OnDataProvided;

        // Logger provided by DI, used for printing information to all logging providers at once
        private readonly ILogger<LegacySerialProvider> _logger;
        private readonly SerialPort _serialPort;
        private readonly Dictionary<DataLabel, int> _schema = [];

        private string _buffer = "";

        private readonly System.Timers.Timer _timer;

        /// <summary>
        /// Create a new instance of SerialProvider
        /// </summary>
        /// <param name="portName">Name of the port from which data will be read</param>
        /// <param name="baudRate">Baud rate, in bps, of the serial port</param>
        /// <param name="parity">Parity of the serial port</param>
        /// <param name="logger"></param>
        public LegacySerialProvider(
            string portName,
            int baudRate,
            Parity parity,
            ILogger<LegacySerialProvider> logger
        )
        {
            _logger = logger;
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
            _timer.Elapsed += HandleDataReceived;
            _timer.Start();
        }

        /// <summary>
        /// Handle data being received on the serial port
        /// </summary>
        /// <param name="sender">The SerialPort object which raised the event</param>
        /// <param name="e"></param>
        private void HandleDataReceived(object? sender, System.Timers.ElapsedEventArgs e)
        {
            _logger.LogInformation("Receiving...");
            _buffer += _serialPort.ReadExisting();
            _buffer = _buffer.TrimStart();
            while (_buffer.Contains('\n'))
            {
                int idx = _buffer.IndexOf('\n');
                string line = _buffer[..idx];
                _buffer = _buffer[(idx + 1)..];
                HandleLineReceived(line);
            }
        }

        private void HandleLineReceived(string line)
        {
            _logger.LogInformation("LINE: {line}", line);
            ;
            // Check for schema message
            if (line.StartsWith("schema", StringComparison.CurrentCultureIgnoreCase))
            {
                ParseSchema(line.ToLower().Replace("schema", ""));
                return;
            }

            if (line.Trim() != "")
                // Process and emit data
                OnDataProvided?.Invoke(WrapInEventData(line));
        }

        /// <summary>
        /// Parses a schema message from the serial port, registering its info in <see cref="_schema"/>
        /// </summary>
        /// <param name="schema">Schema message from the serial port</param>
        /// <exception cref="InvalidDataException">Thrown if <paramref name="schema"/> doesn't fit the expected format</exception>
        private void ParseSchema(string schema)
        {
            _logger.LogInformation("Schema: {data}", schema);
            string[] data = schema.Split(':').Select(x => x.Trim().Trim('[', ']', ';')).ToArray();
            for (int i = 0; i < data.Length; i++)
            {
                DataLabel key = data[i] switch
                {
                    "timestamp" => DataLabel.Timestamp,
                    "pressure" => DataLabel.Pressure,
                    "temperature" => DataLabel.Temperature,
                    "acc_x" => DataLabel.AccelerationX,
                    "acc_y" => DataLabel.AccelerationY,
                    "acc_z" => DataLabel.AccelerationZ,
                    "latitude" => DataLabel.Latitude,
                    "longitude" => DataLabel.Longitude,
                    "altitude" => DataLabel.Altitude,
                    string entry => throw new InvalidDataException(
                        $"Received unknown schema entry {entry} from serial port, consider adding a new item to {nameof(DataLabel)}"
                    ),
                };
                _schema[key] = i;
            }
        }

        /// <summary>
        /// Wrap a message from a serial port in an EventData object
        /// </summary>
        /// <param name="message">The message, formatted as "[<data>]:[<data>]:...;"</param>
        /// <returns>An EventData object containing the message, split into individual pieces</returns>
        private EventData<Dictionary<DataLabel, string>> WrapInEventData(string message)
        {
            try
            {
                _logger.LogInformation("Message: {message}", message);
                // Separate values
                string[] data = [.. message.Split(':').Select(x => x.Trim().Trim('[', ']', ';'))];
                // Build dictionary
                Dictionary<DataLabel, string> dict = _schema
                    .Select(x => (x.Key, data[x.Value]))
                    .ToDictionary();
                float latitude = float.NaN,
                    longitude = float.NaN,
                    altitude = float.NaN;
                if (dict.TryGetValue(DataLabel.Latitude, out string? lat) && lat != "nan")
                    latitude = float.Parse(lat, CultureInfo.InvariantCulture);
                if (dict.TryGetValue(DataLabel.Longitude, out string? lon) && lon != "nan")
                    longitude = float.Parse(lon, CultureInfo.InvariantCulture);
                if (dict.TryGetValue(DataLabel.Altitude, out string? alt) && alt != "nan")
                    altitude = float.Parse(alt, CultureInfo.InvariantCulture);
                // Wrap data
                return new EventData<Dictionary<DataLabel, string>>
                {
                    DataStamp = new DataStamp
                    {
                        Timestamp = int.Parse(
                            dict[DataLabel.Timestamp],
                            CultureInfo.InvariantCulture
                        ),
                        // TODO: get this info from message as well
                        Coordinates = new GPSCoords
                        {
                            Latitude = latitude,
                            Longitude = longitude,
                            Altitude = altitude,
                        },
                    },
                    Data = dict,
                };
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning(
                    "Incomplete schema: {schema}; waiting for schema message.",
                    _schema
                );
                throw;
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
