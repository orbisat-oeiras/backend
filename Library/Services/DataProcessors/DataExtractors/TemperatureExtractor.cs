using backend.Library.Services.DataProviders;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Library.Services.DataProcessors.DataExtractors
{
    /// <summary>
    /// Extracts temperature data from SerialProvider data.
    /// </summary>
    public class TemperatureExtractor : DataExtractorBase<float>
    {
        public TemperatureExtractor(
            [FromKeyedServices(ServiceKeys.DataProvider)]
                IDataProvider<Dictionary<SerialProvider.DataLabel, byte[]>> provider
        )
            : base(provider)
        {
            // Extract temperature data
            _sourceIndexes = [SerialProvider.DataLabel.Temperature];
        }

        protected override float Convert(IEnumerable<byte[]> data) =>
            BitConverter.ToSingle(data.First(), 0);
    }
}
