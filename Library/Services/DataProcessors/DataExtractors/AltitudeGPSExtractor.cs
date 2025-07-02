using backend.Library.Services.DataProviders;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Library.Services.DataProcessors.DataExtractors
{
    public class AltitudeGPSExtractor : DataExtractorBase<float>
    {
        public AltitudeGPSExtractor(
            [FromKeyedServices(ServiceKeys.DataProvider)]
                IDataProvider<Dictionary<SerialProvider.DataLabel, byte[]>> provider
        )
            : base(provider)
        {
            _sourceIndexes = [SerialProvider.DataLabel.Altitude];
        }

        protected override float Convert(IEnumerable<byte[]> data)
        {
            if (data.Any())
            {
                // Convert the first byte array to a float and return it
                return BitConverter.ToSingle(data.First(), 16);
            }
            else
            {
                return float.NaN;
            }
        }
    }
}
