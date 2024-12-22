using backend24.Models;
using backend24.Services.DataProviders;

namespace backend24.Services.EventFinalizers
{
    /// <summary>
    /// Finalizes a temperature event, tagged with "primary/temperature".
    /// </summary>
    public class TemperatureFinalizer : EventFinalizerBase<float>
    {
        public TemperatureFinalizer(
            [FromKeyedServices(ServiceKeys.TemperatureExtractor)] IDataProvider<float> provider
        )
            : base(provider) { }

        protected override EventData<(string, object)> Process(EventData<float> data)
        {
            return new EventData<(string, object)>
            {
                DataStamp = data.DataStamp,
                Data = ("primary/temperature", data.Data),
            };
        }
    }
}
