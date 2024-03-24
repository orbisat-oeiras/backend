using backend24.Models;

using System;
using System.IO.Ports;

namespace backend24.Services.DataProviders
{
	/// <summary>
	/// Provides data read from a serial port.
	/// </summary>
	public sealed class SerialProvider : IDataProvider<string[]>, IDisposable
	{
		public event Action<EventData<string[]>>? OnDataProvided;

		// Logger provided by DI, used for printing information to all logging providers at once
		private readonly ILogger<SerialProvider> _logger;
		private readonly SerialPort _serialPort;

		/// <summary>
		/// Create a new instance of SerialProvider
		/// </summary>
		/// <param name="portName">Name of the port from which data will be read</param>
		/// <param name="baudRate">Baud rate, in bps, of the serial port</param>
		/// <param name="parity">Parity of the serial port</param>
		/// <param name="logger"></param>
		public SerialProvider(string portName, int baudRate, Parity parity, ILogger<SerialProvider> logger) {
			_logger = logger;
			// Note that more options are available for configuring a SerialPort,
			// namely data bits, stop bits and handshake. I have no idea what those
			// are, and am very likely to ever change them in the radio modules
			// configuration, so it's most probably (read hopefully) fine to keep
			// them at the default value.
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
			string portData = sp.ReadExisting();
			//_logger.LogInformation(portData);
			// Process and emit data
            OnDataProvided?.Invoke(WrapInEventData(portData));
		}

		/// <summary>
		/// Wrap a message from a serial port in an EventData object
		/// </summary>
		/// <param name="message">The message, formatted as "[<data>]:[<data>]:...;"</param>
		/// <returns>An EventData object containign the message, split into individual pieces</returns>
		private EventData<string[]> WrapInEventData(string message) {
			// Separate values
			string[] data = message.Split(':').Select(x => x.Trim().Trim('[', ']', ';')).ToArray();
			// Wrap data
			return new EventData<string[]> {
				DataStamp = new DataStamp {
					Timestamp = int.Parse(data[0]),
					// TODO: get this info from message as well
					Coordinates = new GPSCoords {
						Latitude = 0,
						Longitude = 0,
						Altitude = 0
					}
				},
				Data = data
			};
		}

		public void Dispose() {
			// Close the serial port so it can be used by other apps
			_serialPort.Close();
		}
	}
}
