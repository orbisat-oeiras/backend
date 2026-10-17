using backend.Library.Services.DataProviders;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Library.Services.DataProcessors.DataExtractors
{
    /// <summary>
    /// Extracts Acceleration X data from SerialProvider data.
    /// </summary>
    public class Mission1Extractor : DataExtractorBase<float>
    {
        public Mission1Extractor(
            [FromKeyedServices(ServiceKeys.DataProvider)]
                IDataProvider<Dictionary<SerialProvider.DataLabel, byte[]>> provider
        )
            : base(provider)
        {
            // Extract Acceleration X data
            _sourceIndexes = [SerialProvider.DataLabel.Mission1];
        }

        protected override float Convert(IEnumerable<byte[]> data) =>
            BitConverter.ToSingle(data.First(), 0);
    }
}
