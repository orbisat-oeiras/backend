using System.Threading.Tasks;
using backend.Library.Services.EventFinalizers;

namespace backend.Server.Controllers
{
    /// <summary>
    /// Controller for handling CSV file creation and processing.
    /// </summary>
    public class CsvController(
        ILogger<CsvController> logger,
        IEnumerable<IFinalizedProvider> eventFinalizers
    ) : BackgroundService
    {
        private readonly ILogger<CsvController> _logger = logger;
        private readonly IEnumerable<IFinalizedProvider> _eventFinalizers = eventFinalizers;

        // This gets automatically run because of the BackgroundService base class.
        // Implemenation in Program.cs is done with builder.Services.AddHostedService<CsvController>() (line 107).
        // Idk if this previous comment is necessary but it's good to know.

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            bool hasHeader = false;
            string filePath = $"Data\\{DateTime.Now:yy-MM-dd-HH-mm-ss}.csv";
            // Create the directory if it doesn't exist
            Directory.CreateDirectory("Data");

            _logger.LogInformation("CSV file path: {filePath}", filePath);
            StreamWriter sw = new(filePath, true);
            List<string> tags = [];

            foreach (IFinalizedProvider eventFinalizer in _eventFinalizers)
            {
                // Subscribe to finalizers
                eventFinalizer.OnDataProvided += async payload =>
                {
                    if (!hasHeader)
                    {
                        tags.Add(payload.Data.tag);
                        if (tags.Count == _eventFinalizers.Count())
                        {
                            await sw.WriteLineAsync(
                                $"{string.Join(",", tags).Replace("primary/", "")},Timestamp"
                            );
                            await sw.FlushAsync();
                            hasHeader = true;
                            _logger.LogInformation("CSV header written.");
                        }
                    }
                    else
                    {
                        await sw.WriteAsync(payload.Data.content + ",");
                        if (payload.Data.tag == tags.Last())
                        {
                            await sw.WriteLineAsync($"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
                            await sw.FlushAsync();
                        }
                    }
                };
            }
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, cancellationToken);
            }
        }
    }
}
