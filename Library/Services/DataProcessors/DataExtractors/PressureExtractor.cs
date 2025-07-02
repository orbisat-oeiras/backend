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
                IDataProvider<Dictionary<SerialProvider.DataLabel, byte[]>> provider
        )
            : base(provider)
        {
            // Extract pressure data
            _sourceIndexes = [SerialProvider.DataLabel.Pressure];
        }

        protected override float Convert(IEnumerable<byte[]> data) =>
            BitConverter.ToSingle(data.First(), 0);
    }
}
