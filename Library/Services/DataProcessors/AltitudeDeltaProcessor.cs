using backend.Library.Models;
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

        private float _last = float.NaN;

        protected override EventData<float> Process(EventData<float> data)
        {
            float altitudedelta = 0;
            if (!float.IsNaN(_last))
                altitudedelta = data.Data - _last;
            _last = data.Data;

            return data with
            {
                Data = altitudedelta,
            };
        }
    }
}
