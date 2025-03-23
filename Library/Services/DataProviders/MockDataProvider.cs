using System.IO.Ports;
using System.Timers;
using backend.Library.Models;
using Microsoft.Extensions.Logging;

namespace backend.Library.Services.DataProviders
{
    /// <summary>
    /// Provides data read from a serial port.
    /// </summary>
    public sealed class MockDataProvider
        : IDataProvider<Dictionary<SerialProvider.DataLabel, string>>,
            IDisposable
    {
        private readonly Random _random = new();
        public event Action<
            EventData<Dictionary<SerialProvider.DataLabel, string>>
        >? OnDataProvided;
        private readonly float _altitude = 1000;
        private readonly float _temperature = 15;
        private readonly float _pressure = 100000;

        // Logger provided by DI, used for printing information to all logging providers at once
        private readonly ILogger<MockDataProvider> _logger;
        private readonly SerialPort _serialPort;
        private readonly Dictionary<SerialProvider.DataLabel, int> _schema = new();

        private readonly System.Timers.Timer _timer;

        private Dictionary<SerialProvider.DataLabel, string> _lastData = new();

        public MockDataProvider(ILogger<MockDataProvider> logger)
        {
            _serialPort = new SerialPort();
            _logger = logger;
            _timer = new System.Timers.Timer(1000) { AutoReset = true };
            _timer.Elapsed += GenerateMockData;
            _timer.Start();
            _logger.LogInformation("MockDataProvider started");
        }

        private void GenerateMockData(object? sender, ElapsedEventArgs e)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _lastData = new Dictionary<SerialProvider.DataLabel, string>
            {
                { SerialProvider.DataLabel.Timestamp, now.ToString() },
                { SerialProvider.DataLabel.Pressure, _pressure.ToString() },
                { SerialProvider.DataLabel.Temperature, _altitude.ToString() },
                { SerialProvider.DataLabel.AccelerationX, _random.Next(-10, 10).ToString() },
                { SerialProvider.DataLabel.AccelerationY, _random.Next(-10, 10).ToString() },
                { SerialProvider.DataLabel.AccelerationZ, _random.Next(-10, 10).ToString() },
                { SerialProvider.DataLabel.Latitude, _random.Next(-90, 90).ToString() },
                { SerialProvider.DataLabel.Longitude, _random.Next(-180, 180).ToString() },
                { SerialProvider.DataLabel.Altitude, _altitude.ToString() },
            };
            OnDataProvided?.Invoke(
                new EventData<Dictionary<SerialProvider.DataLabel, string>>
                {
                    DataStamp = new DataStamp
                    {
                        Timestamp = long.Parse(_lastData[SerialProvider.DataLabel.Timestamp]),
                        Coordinates = new GPSCoords
                        {
                            Latitude = float.Parse(_lastData[SerialProvider.DataLabel.Latitude]),
                            Longitude = float.Parse(_lastData[SerialProvider.DataLabel.Longitude]),
                            Altitude = float.Parse(_lastData[SerialProvider.DataLabel.Altitude]),
                        },
                    },
                    Data = _lastData,
                }
            );
            _logger.LogInformation($"Pressure = {_pressure}, Temperature = {_altitude}");
        }

        public Dictionary<SerialProvider.DataLabel, string> GetData()
        {
            return _lastData;
        }

        public void Dispose()
        {
            _timer.Stop();
            _timer.Dispose();
        }
    }
}
