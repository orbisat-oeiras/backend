using System.Globalization;
using backend.Library.Services;
using backend.Library.Services.DataProviders;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Library.Services.DataProcessors.DataExtractors
{
    /// <summary>
    /// Extracts pressure data from SerialProvider data.
    /// </summary>
    public class PressureExtractor : DataExtractorBase<float>
    {
        public PressureExtractor(
            [FromKeyedServices(ServiceKeys.DataProvider)]
                IDataProvider<Dictionary<LegacySerialProvider.DataLabel, string>> provider
        )
            : base(provider)
        {
            // Extract pressure data
            _sourceIndexes = [LegacySerialProvider.DataLabel.Pressure];
        }

        protected override float Convert(IEnumerable<string> data) =>
            float.Parse(data.First(), CultureInfo.InvariantCulture);
    }
}
