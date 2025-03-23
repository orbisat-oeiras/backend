using System.IO.Ports;
using System.Timers;
using backend.Library.Models;
using Microsoft.Extensions.Logging;

namespace backend.Library.Services.DataProviders
{
    /// <summary>
    /// Provides fake data created on the fly.
    /// </summary>
    public sealed class MockDataProvider
        : IDataProvider<Dictionary<SerialProvider.DataLabel, string>>,
            IDisposable
    {
        private readonly Random _random = new();
        public event Action<
            EventData<Dictionary<SerialProvider.DataLabel, string>>
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
            _pressure += _pressure + _random.Next(-100, 100);
            _altitude += _altitude + _random.Next(-10, 10);
            _temperature += _temperature + _random.Next(-1, 1);
            _accelerationX += _random.Next(-1, 1);
            _accelerationY += _random.Next(-1, 1);
            _accelerationZ += _random.Next(-1, 1);
            _latitude += _random.Next(36, 37);
            _longitude += _random.Next(-25, -26);

            Dictionary<SerialProvider.DataLabel, string> lastData = new Dictionary<
                SerialProvider.DataLabel,
                string
            >
            {
                { SerialProvider.DataLabel.Timestamp, now.ToString() },
                { SerialProvider.DataLabel.Pressure, _pressure.ToString() },
                { SerialProvider.DataLabel.Temperature, _altitude.ToString() },
                { SerialProvider.DataLabel.AccelerationX, _accelerationX.ToString() },
                { SerialProvider.DataLabel.AccelerationY, _accelerationY.ToString() },
                { SerialProvider.DataLabel.AccelerationZ, _accelerationZ.ToString() },
                { SerialProvider.DataLabel.Latitude, _latitude.ToString() },
                { SerialProvider.DataLabel.Longitude, _longitude.ToString() },
                { SerialProvider.DataLabel.Altitude, _altitude.ToString() },
            };
            OnDataProvided?.Invoke(
                new EventData<Dictionary<SerialProvider.DataLabel, string>>
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
