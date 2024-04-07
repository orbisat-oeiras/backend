using backend24.Models;

using System.IO.Ports;

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
		public enum DataLabel {
			Timestamp,
			Pressure,
			Temperature
		}

		public event Action<EventData<Dictionary<DataLabel, string>>>? OnDataProvided;

		// Logger provided by DI, used for printing information to all logging providers at once
		private readonly ILogger<SerialProvider> _logger;
		private readonly SerialPort _serialPort;
		private readonly Dictionary<DataLabel, int> _schema;

		/// <summary>
		/// Create a new instance of SerialProvider
		/// </summary>
		/// <param name="portName">Name of the port from which data will be read</param>
		/// <param name="baudRate">Baud rate, in bps, of the serial port</param>
		/// <param name="parity">Parity of the serial port</param>
		/// <param name="logger"></param>
		public SerialProvider(string portName, int baudRate, Parity parity, ILogger<SerialProvider> logger) {
			_logger = logger;
			// Initialize schema with invalid values
			_schema = new Dictionary<DataLabel, int> {
				{DataLabel.Timestamp, -1 },
				{DataLabel.Pressure, -1},
				{DataLabel.Temperature, -1},
			};
			// Note that more options are available for configuring a SerialPort,
			// namely data bits, stop bits and handshake. I have no idea what those
			// are, and am very likely to ever change them in the radio modules
			// configuration, so it's most probably (read hopefully) fine to keep
			// them at the default value.
			// TODO: research about choosing a good baud rate
			_serialPort = new SerialPort(portName, baudRate, parity) {
				// I have no clue what a reasonable value for this is
				ReadTimeout = 500,
				WriteTimeout = 500
			};
			// Set up event listeners
			_serialPort.DataReceived += HandleDataReceived;
			// Open the port
			_serialPort.Open();
		}

		/// <summary>
		/// Handle data being received on the serial port
		/// </summary>
		/// <param name="sender">The SerialPort object which raised the event</param>
		/// <param name="e"></param>
		private void HandleDataReceived(object sender, SerialDataReceivedEventArgs e) {
			// This shouldn't be necessary - what else is going to send a SerialPort event??
			SerialPort sp = (SerialPort)sender;
			// Read from the port
			string portData = sp.ReadLine();
			// Check for schema message
			if(portData.StartsWith("schema", StringComparison.CurrentCultureIgnoreCase)) {
				ParseSchema(portData.ToLower().Replace("schema", ""));
				return;
			}

			// Start processing data only after a schema has arrived
			if(!_schema.ContainsValue(-1)) {
				// Process and emit data
				OnDataProvided?.Invoke(WrapInEventData(portData));
			} else {
				_logger.LogWarning("Waiting for schema message");
			}
		}

		/// <summary>
		/// Parses a schema message from the serial port, registering its info in <see cref="_schema"/>
		/// </summary>
		/// <param name="schema">Schema message from the serial port</param>
		/// <exception cref="InvalidDataException">Thrown if <paramref name="schema"/> doesn't fit the expected format</exception>
		private void ParseSchema(string schema) {
			string[] data = schema.Split(':').Select(x => x.Trim().Trim('[', ']', ';')).ToArray();
            for(int i = 0; i < data.Length; i++) {
				DataLabel key = data[i] switch {
					"timestamp" => DataLabel.Timestamp,
					"pressure" => DataLabel.Pressure,
					"temperature" => DataLabel.Temperature,
					string entry => throw new InvalidDataException($"Received unknown schema entry {entry} from serial port, consider adding a new item to {nameof(DataLabel)}")
				};
				_schema[key] = i;
			}

			if(_schema.ContainsValue(-1)) throw new InvalidDataException($"Schema received from serial port didn't contain entry for every {nameof(DataLabel)}");
		}

		/// <summary>
		/// Wrap a message from a serial port in an EventData object
		/// </summary>
		/// <param name="message">The message, formatted as "[<data>]:[<data>]:...;"</param>
		/// <returns>An EventData object containing the message, split into individual pieces</returns>
		private EventData<Dictionary<DataLabel, string>> WrapInEventData(string message) {
			// Separate values
			string[] data = message.Split(':').Select(x => x.Trim().Trim('[', ']', ';')).ToArray();
			// Build dictionary
			Dictionary<DataLabel, string> dict = _schema.Select(x => (x.Key, data[x.Value])).ToDictionary();
			// Wrap data
			return new EventData<Dictionary<DataLabel, string>> {
				DataStamp = new DataStamp {
					Timestamp = int.Parse(data[0]),
					// TODO: get this info from message as well
					Coordinates = new GPSCoords {
						Latitude = 0,
						Longitude = 0,
						Altitude = 0
					}
				},
				Data = dict
			};
		}

		public void Dispose() {
			// Close the serial port so it can be used by other apps
			_serialPort.Close();
		}
	}
}
