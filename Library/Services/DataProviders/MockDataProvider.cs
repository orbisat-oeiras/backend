using System.Timers;
using backend.Library.Models;
using Microsoft.Extensions.Logging;

namespace backend.Library.Services.DataProviders
{
    /// <summary>
    /// Provides fake data created on the fly.
    /// </summary>
    public sealed class MockDataProvider
        : IDataProvider<Dictionary<SerialProvider.DataLabel, byte[]>>,
            IDisposable
    {
        private readonly Random _random = new();
        public event Action<
            EventData<Dictionary<SerialProvider.DataLabel, byte[]>>
        >? OnDataProvided;
        private float _altitude = 1000;
        private float _temperature = 20;
        private float _pressure = 100000;
        private float _humidity = 50;
        private float _accelerationX,
            _accelerationY,
            _accelerationZ;
        private float _latitude,
            _longitude;

        // Logger provided by DI, used for printing information to all logging providers at once
        private readonly ILogger<MockDataProvider> _logger;

        private readonly System.Timers.Timer _timer;

        public MockDataProvider(ILogger<MockDataProvider> logger)
        {
            _logger = logger;
            _timer = new System.Timers.Timer(1000) { AutoReset = true };
            _timer.Elapsed += GenerateMockData;
            _timer.Start();
            _logger.LogInformation("MockDataProvider started");
        }

        private void GenerateMockData(object? sender, ElapsedEventArgs e)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _pressure += _random.Next(-100, 100);
            _altitude += _random.Next(-10, 10);
            _temperature += _random.Next(-1, 1);
            _humidity += _random.Next(-1, 1);
            _accelerationX += _random.Next(-1, 1) * 0.1f;
            _accelerationY += _random.Next(-1, 1) * 0.1f;
            _accelerationZ += _random.Next(-1, 1) * 0.1f;
            _latitude = 36;
            _longitude = -25;

            Dictionary<SerialProvider.DataLabel, byte[]> lastData = new Dictionary<
                SerialProvider.DataLabel,
                byte[]
            >
            {
                { SerialProvider.DataLabel.Timestamp, BitConverter.GetBytes(now) },
                { SerialProvider.DataLabel.Pressure, BitConverter.GetBytes(_pressure) },
                { SerialProvider.DataLabel.Temperature, BitConverter.GetBytes(_temperature) },
                { SerialProvider.DataLabel.Humidity, BitConverter.GetBytes(_humidity) },
                { SerialProvider.DataLabel.Latitude, BitConverter.GetBytes(_latitude) },
                { SerialProvider.DataLabel.Longitude, BitConverter.GetBytes(_longitude) },
            };
            OnDataProvided?.Invoke(
                new EventData<Dictionary<SerialProvider.DataLabel, byte[]>>
                {
                    DataStamp = new DataStamp
                    {
                        Timestamp = (ulong)now,
                        Coordinates = new GPSCoords
                        {
                            Latitude = _latitude,
                            Longitude = _longitude,
                        },
                    },
                    Data = lastData,
                }
            );
            _logger.LogInformation("Generated Mock Data");
        }

        public void Dispose()
        {
            _timer.Stop();
            _timer.Dispose();
        }
    }
}
