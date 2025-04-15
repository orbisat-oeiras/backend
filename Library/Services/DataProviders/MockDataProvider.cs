using System.Timers;
using backend.Library.Models;
using Microsoft.Extensions.Logging;

namespace backend.Library.Services.DataProviders
{
    /// <summary>
    /// Provides fake data created on the fly.
    /// </summary>
    public sealed class MockDataProvider
        : IDataProvider<Dictionary<LegacySerialProvider.DataLabel, string>>,
            IDisposable
    {
        private readonly Random _random = new();
        public event Action<
            EventData<Dictionary<LegacySerialProvider.DataLabel, string>>
        >? OnDataProvided;
        private float _altitude = 1000;
        private float _temperature = 20;
        private float _pressure = 100000;
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
            _accelerationX += _random.Next(-1, 1) * 0.1f;
            _accelerationY += _random.Next(-1, 1) * 0.1f;
            _accelerationZ += _random.Next(-1, 1) * 0.1f;
            _latitude = 36;
            _longitude = -25;

            Dictionary<LegacySerialProvider.DataLabel, string> lastData = new Dictionary<
                LegacySerialProvider.DataLabel,
                string
            >
            {
                { LegacySerialProvider.DataLabel.Timestamp, now.ToString() },
                { LegacySerialProvider.DataLabel.Pressure, _pressure.ToString() },
                { LegacySerialProvider.DataLabel.Temperature, _temperature.ToString() },
                { LegacySerialProvider.DataLabel.AccelerationX, _accelerationX.ToString() },
                { LegacySerialProvider.DataLabel.AccelerationY, _accelerationY.ToString() },
                { LegacySerialProvider.DataLabel.AccelerationZ, _accelerationZ.ToString() },
                { LegacySerialProvider.DataLabel.Latitude, _latitude.ToString() },
                { LegacySerialProvider.DataLabel.Longitude, _longitude.ToString() },
                { LegacySerialProvider.DataLabel.Altitude, _altitude.ToString() },
            };
            OnDataProvided?.Invoke(
                new EventData<Dictionary<LegacySerialProvider.DataLabel, string>>
                {
                    DataStamp = new DataStamp
                    {
                        Timestamp = now,
                        Coordinates = new GPSCoords
                        {
                            Latitude = _latitude,
                            Longitude = _longitude,
                            Altitude = _altitude,
                        },
                    },
                    Data = lastData,
                }
            );
            _logger.LogInformation("Genereted Mock Data");
        }

        public void Dispose()
        {
            _timer.Stop();
            _timer.Dispose();
        }
    }
}
