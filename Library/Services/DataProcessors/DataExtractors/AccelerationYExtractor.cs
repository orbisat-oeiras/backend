using System.Globalization;
using backend.Library.Services;
using backend.Library.Services.DataProviders;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Library.Services.DataProcessors.DataExtractors
{
    /// <summary>
    /// Extracts Acceleration Y data from SerialProvider data.
    /// </summary>
    public class AccelerationYExtractor : DataExtractorBase<float>
    {
        public AccelerationYExtractor(
            [FromKeyedServices(ServiceKeys.DataProvider)]
                IDataProvider<Dictionary<SerialProvider.DataLabel, byte[]>> provider
        )
            : base(provider)
        {
            // Extract Acceleration Y data
            _sourceIndexes = [SerialProvider.DataLabel.AccelerationY];
        }

        protected override float Convert(IEnumerable<byte[]> data) =>
            BitConverter.ToSingle(data.First(), 4);
    }
}
