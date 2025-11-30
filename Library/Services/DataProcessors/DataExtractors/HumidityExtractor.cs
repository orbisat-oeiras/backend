using backend.Library.Services.DataProviders;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Library.Services.DataProcessors.DataExtractors
{
    /// <summary>
    /// Extracts humidity data from SerialProvider data.
    /// </summary>
    public class HumidityExtractor : DataExtractorBase<float>
    {
        public HumidityExtractor(
            [FromKeyedServices(ServiceKeys.DataProvider)]
                IDataProvider<Dictionary<SerialProvider.DataLabel, byte[]>> provider
        )
            : base(provider)
        {
            // Extract humidity data
            _sourceIndexes = [SerialProvider.DataLabel.Humidity];
        }

        protected override float Convert(IEnumerable<byte[]> data) =>
            BitConverter.ToSingle(data.First(), 0);
    }
}
