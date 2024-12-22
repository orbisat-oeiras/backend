using backend24.Models;
using backend24.Services.DataProviders;

namespace backend24.Services.EventFinalizers
{
    public class PressureFinalizer : EventFinalizerBase<float>
    {
        public PressureFinalizer(
            [FromKeyedServices(ServiceKeys.PressureExtractor)] IDataProvider<float> provider
        )
            : base(provider) { }

        protected override EventData<(string, object)> Process(EventData<float> data)
        {
            return new EventData<(string, object)>
            {
                DataStamp = data.DataStamp,
                Data = ("primary/pressure", data.Data),
            };
        }
    }
}
