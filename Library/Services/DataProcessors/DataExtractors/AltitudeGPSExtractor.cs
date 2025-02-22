using backend.Library.Services;
using backend.Library.Services.DataProviders;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Library.Services.DataProcessors.DataExtractors
{
    public class AltitudeGPSExtractor : DataExtractorBase<float>
    {
        public AltitudeGPSExtractor(
            [FromKeyedServices(ServiceKeys.SerialProvider)]
                IDataProvider<Dictionary<SerialProvider.DataLabel, string>> provider
        )
            : base(provider)
        {
            _sourceIndexes = [SerialProvider.DataLabel.Altitude];
        }

        protected override float Convert(IEnumerable<string> data)
        {
            float altitude = 0;
            if (data.First() != "nan")
                altitude = float.Parse(data.First());
            return altitude;
        }
    }
}
