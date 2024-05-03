using backend24.Models;

using System;
using System.IO.Ports;
using System.Text.RegularExpressions;

namespace backend24.Services.DataProviders
{
    /// <summary>
    /// Provides data read from a serial port.
    /// </summary>
    public sealed class SerialProvider : IDataProvider<Dictionary<SerialProvider.DataLabel, string>>, IDisposable
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
            Altitude
        }

        public event Action<EventData<Dictionary<DataLabel, string>>>? OnDataProvided;

        // Logger provided by DI, used for printing information to all logging providers at once
        private readonly ILogger<SerialProvider> _logger;
        private readonly SerialPort _serialPort;
        private readonly Dictionary<DataLabel, int> _schema;

        private string _buffer = "";
		private Thread _collThread;
        
		private System.Timers.Timer _timer;

        /// <summary>
        /// Create a new instance of SerialProvider
        /// </summary>
        /// <param name="portName">Name of the port from which data will be read</param>
        /// <param name="baudRate">Baud rate, in bps, of the serial port</param>
        /// <param name="parity">Parity of the serial port</param>
        /// <param name="logger"></param>
        public SerialProvider(string portName, int baudRate, Parity parity, ILogger<SerialProvider> logger)
        {
            _logger = logger;
            // Initialize schema with invalid values
            _schema = new Dictionary<DataLabel, int> {
                {DataLabel.Timestamp, -1 },
                {DataLabel.Pressure, -1},
                {DataLabel.Temperature, -1},
                {DataLabel.AccelerationX, -1},
                {DataLabel.AccelerationY, -1},
                {DataLabel.AccelerationZ, -1},
                {DataLabel.Latitude, -1},
                {DataLabel.Longitude, -1},
                {DataLabel.Altitude, -1},
            };
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
                WriteTimeout = 500
            };
            // Open the port
            _serialPort.Open();
			// Set up event listeners
			// _serialPort.DataReceived += HandleDataReceived;
			//_collThread = new Thread(CollectionThread);
			//_collThread.Start();
            _timer = new System.Timers.Timer(500);
            _timer.AutoReset = true;
            _timer.Elapsed += HandleDataReceived;
            _timer.Start();
        }

        private void CollectionThread(){
            while(true) {
				lock(_buffer) {
					_buffer += _serialPort.ReadExisting();
				}
				Thread.Sleep(50);
			}
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
			while(_buffer.Contains('\n')){
                int idx = _buffer.IndexOf("\n");
				string line = _buffer[..idx];
                _buffer = _buffer.Remove(0, line.Length);
                HandleLineReceived(line);
                AppendToFile(line);
            }
		}

        private void HandleLineReceived(string line)
        {
            _logger.LogInformation("LINE: {line}", line);;
            // Check for schema message
            if (line.StartsWith("schema", StringComparison.CurrentCultureIgnoreCase))
            {
                ParseSchema(line.ToLower().Replace("schema", ""));
                return;
            }

            // Start processing data only after a schema has arrived
			//_logger.LogInformation("Should");
            if (!_schema.ContainsValue(-1))
            {
                if (line.Trim() != "")
                {
                    // Process and emit data
                    OnDataProvided?.Invoke(WrapInEventData(line));
                }
            }
            else
            {
                //_logger.LogWarning("Waiting for schema message");
            }
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
                    string entry => throw new InvalidDataException($"Received unknown schema entry {entry} from serial port, consider adding a new item to {nameof(DataLabel)}")
                };
                _schema[key] = i;
            }

            if (_schema.ContainsValue(-1)) throw new InvalidDataException($"Schema received from serial port didn't contain entry for every {nameof(DataLabel)}");
        }

        /// <summary>
        /// Wrap a message from a serial port in an EventData object
        /// </summary>
        /// <param name="message">The message, formatted as "[<data>]:[<data>]:...;"</param>
        /// <returns>An EventData object containing the message, split into individual pieces</returns>
        private EventData<Dictionary<DataLabel, string>> WrapInEventData(string message)
        {
            _logger.LogInformation("Message: {message}", message);
            // Separate values
            string[] data = message.Split(':').Select(x => x.Trim().Trim('[', ']', ';')).ToArray();
            // Build dictionary
            Dictionary<DataLabel, string> dict = _schema.Select(x => (x.Key, data[x.Value])).ToDictionary();
            // Wrap data
            return new EventData<Dictionary<DataLabel, string>>
            {
                DataStamp = new DataStamp
                {
                    Timestamp = int.Parse(data[0]),
                    // TODO: get this info from message as well
                    Coordinates = new GPSCoords
                    {
                        Latitude = 0,
                        Longitude = 0,
                        Altitude = 0
                    }
                },
                Data = dict
            };
        }

		private void AppendToFile(string toAppend) {
			string filePath = @"D:\escola\20232024\clube\cansat\code\datasave";
			File.AppendAllText(filePath, toAppend);
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
