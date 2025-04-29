using System.Globalization;
using backend.Library.Services;
using backend.Library.Services.DataProviders;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Library.Services.DataProcessors.DataExtractors
{
    /// <summary>
    /// Extracts Acceleration Z data from SerialProvider data.
    /// </summary>
    public class AccelerationZExtractor : DataExtractorBase<float>
    {
        public AccelerationZExtractor(
            [FromKeyedServices(ServiceKeys.DataProvider)]
                IDataProvider<Dictionary<SerialProvider.DataLabel, byte[]>> provider
        )
            : base(provider)
        {
            // Extract Acceleration Z data
            _sourceIndexes = [SerialProvider.DataLabel.AccelerationZ];
        }

        protected override float Convert(IEnumerable<byte[]> data) =>
            BitConverter.ToSingle(data.First(), 8);
    }
}
