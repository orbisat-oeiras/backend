using backend24.Models;
using backend24.Services.DataProviders;

namespace backend24.Services.DataProcessors
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
