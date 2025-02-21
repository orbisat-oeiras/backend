using backend.Library.Models;
using backend.Library.Services;
using backend.Library.Services.DataProviders;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Library.Services.EventFinalizers
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
