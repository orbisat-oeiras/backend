using System.Globalization;
using backend.Library.Models;
using backend.Library.Services.EventFinalizers;

namespace backend.Server.Controllers
{
    /// <summary>
    /// Controller for handling CSV file creation and processing.
    /// </summary>
    public class CsvController : BackgroundService
    {
        private readonly ILogger<CsvController> _logger;
        private readonly IEnumerable<IFinalizedProvider> _eventFinalizers;

        private string? _currentTimestamp = null;
        private readonly string[] _data = new string[10];
        private StreamWriter? _sw;
        private readonly object _lock = new();

        // This works because of the BackgroundService base class
        public CsvController(
            ILogger<CsvController> logger,
            IEnumerable<IFinalizedProvider> eventFinalizers
        )
        {
            _logger = logger;
            _eventFinalizers = eventFinalizers;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            string directory = "Data";
            string fileName = $"{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.csv";
            string filePath = Path.Combine(directory, fileName);
            Directory.CreateDirectory(directory);
            _logger.LogInformation("CSV file path: {filePath}", filePath);

            try
            {
                using (_sw = new StreamWriter(filePath, true))
                {
                    await _sw.WriteLineAsync(
                        "pressure,temperature,altitude,humidity,latitude,longitude,altitudegps,velocity,altitudedelta,timestamp"
                    );
                    await _sw.FlushAsync();

                    for (int i = 0; i < _data.Length; i++)
                        _data[i] = string.Empty;

                    foreach (IFinalizedProvider eventFinalizer in _eventFinalizers)
                    {
                        eventFinalizer.OnDataProvided += HandleData; // Direct subscription
                    }

                    await Task.Delay(Timeout.Infinite, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("CsvController stopping due to cancellation.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during CsvController execution.");
            }
            finally
            {
                _logger.LogInformation("CsvController cleaning up subscriptions.");
                foreach (IFinalizedProvider eventFinalizer in _eventFinalizers)
                {
                    eventFinalizer.OnDataProvided -= HandleData; // Direct unsubscription
                }
            }
        }

        private void HandleData(EventData<(string tag, object content)> payload)
        {
            if (_sw == null)
            {
                _logger.LogWarning("HandleData called but StreamWriter is null (disposed?).");
                return;
            }

            try
            {
                string incomingTimestamp = payload.DataStamp.Timestamp.ToString();
                bool lineWritten = false;

                lock (_lock)
                {
                    if (_sw == null)
                        return;

                    if (_currentTimestamp != null && incomingTimestamp != _currentTimestamp)
                    {
                        _sw.WriteLine(string.Join(",", _data));
                        _sw.Flush();
                        lineWritten = true;

                        for (int i = 0; i < _data.Length; i++)
                            _data[i] = string.Empty;
                    }
                    _currentTimestamp = incomingTimestamp;

                    switch (payload.Data.tag.ToString().ToLowerInvariant())
                    {
                        case "pressure":
                            _data[0] =
                                payload.Data.content?.ToString()?.Replace(",", ".") ?? string.Empty;
                            break;
                        case "temperature":
                            _data[1] =
                                payload.Data.content?.ToString()?.Replace(",", ".") ?? string.Empty;
                            break;
                        case "altitude":
                            _data[2] =
                                payload.Data.content?.ToString()?.Replace(",", ".") ?? string.Empty;
                            break;
                        case "humidity":
                            _data[3] =
                                payload.Data.content?.ToString()?.Replace(",", ".") ?? string.Empty;
                            break;
                        case "velocity":
                            _data[7] =
                                payload.Data.content?.ToString()?.Replace(",", ".") ?? string.Empty;
                            break;
                        case "altitudedelta":
                            _data[8] =
                                payload.Data.content?.ToString()?.Replace(",", ".") ?? string.Empty;
                            break;
                        default:
                            _logger.LogWarning("Unknown tag: {tag}", payload.Data.tag.ToString());
                            break;
                    }

                    // Always update GPS coordinates from DataStamp
                    _data[4] = double.IsNaN(payload.DataStamp.Coordinates.Latitude)
                        ? "0"
                        : payload.DataStamp.Coordinates.Latitude.ToString(
                            CultureInfo.InvariantCulture
                        );
                    _data[5] = double.IsNaN(payload.DataStamp.Coordinates.Longitude)
                        ? "0"
                        : payload.DataStamp.Coordinates.Longitude.ToString(
                            CultureInfo.InvariantCulture
                        );
                    _data[6] = float.IsNaN(payload.DataStamp.Coordinates.Altitude)
                        ? "0"
                        : payload.DataStamp.Coordinates.Altitude.ToString(
                            CultureInfo.InvariantCulture
                        );
                    _data[9] = payload.DataStamp.Timestamp.ToString();
                }

                if (lineWritten)
                {
                    _logger.LogInformation("Wrote data row for timestamp {ts}", _currentTimestamp);
                }
            }
            catch (ObjectDisposedException)
            {
                _logger.LogWarning(
                    "ObjectDisposedException in HandleData. Service might be shutting down."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing payload in HandleData for tag {tag}",
                    payload.Data.tag
                );
            }
        }
    }
}
