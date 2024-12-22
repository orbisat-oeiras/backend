using backend24.Models;
using backend24.Services.DataProviders;

namespace backend24.Services.EventFinalizers
{
    public class AltitudeDeltaFinalizer : EventFinalizerBase<float>
    {
        public AltitudeDeltaFinalizer(
            [FromKeyedServices(ServiceKeys.AltitudeDeltaProcessor)] IDataProvider<float> provider
        )
            : base(provider) { }

        protected override EventData<(string, object)> Process(EventData<float> data)
        {
            return new EventData<(string, object)>
            {
                DataStamp = data.DataStamp,
                Data = ("primary/altitudedelta", data.Data),
            };
        }
    }
}
