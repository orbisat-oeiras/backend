using backend.Library.Models;
using backend.Library.Services;
using backend.Library.Services.DataProviders;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Library.Services.DataProcessors
{
    public class AltitudeDeltaProcessor : DataProcessorBase<float, float>
    {
        public AltitudeDeltaProcessor(
            [FromKeyedServices(ServiceKeys.AltitudeExtractor)] IDataProvider<float> provider
        )
            : base(provider) { }

        protected override EventData<float> Process(EventData<float> data)
        {
            return data with { Data = data.Data - data.DataStamp.Coordinates.Altitude };
        }
    }
}
