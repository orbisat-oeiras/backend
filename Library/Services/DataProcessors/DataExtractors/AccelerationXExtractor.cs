using System.Globalization;
using backend.Library.Services;
using backend.Library.Services.DataProviders;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Library.Services.DataProcessors.DataExtractors
{
    /// <summary>
    /// Extracts Acceleration X data from SerialProvider data.
    /// </summary>
    public class AccelerationXExtractor : DataExtractorBase<float>
    {
        public AccelerationXExtractor(
            [FromKeyedServices(ServiceKeys.DataProvider)]
                IDataProvider<Dictionary<SerialProvider.DataLabel, string>> provider
        )
            : base(provider)
        {
            // Extract Acceleration X data
            _sourceIndexes = [SerialProvider.DataLabel.AccelerationX];
        }

        protected override float Convert(IEnumerable<string> data) =>
            float.Parse(data.First(), CultureInfo.InvariantCulture);
    }
}
