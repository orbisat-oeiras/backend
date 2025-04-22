using System.Globalization;
using backend.Library.Services;
using backend.Library.Services.DataProviders;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Library.Services.DataProcessors.DataExtractors
{
    /// <summary>
    /// Extracts pressure data from SerialProvider data.
    /// </summary>
    public class HumidityExtractor : DataExtractorBase<float>
    {
        public HumidityExtractor(
            [FromKeyedServices(ServiceKeys.DataProvider)]
                IDataProvider<Dictionary<SerialProvider.DataLabel, string>> provider
        )
            : base(provider)
        {
            // Extract pressure data
            _sourceIndexes = [SerialProvider.DataLabel.Humidity];
        }

        protected override float Convert(IEnumerable<string> data) =>
            float.Parse(data.First(), CultureInfo.InvariantCulture);
    }
}
