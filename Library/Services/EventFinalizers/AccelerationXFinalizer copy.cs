using backend.Library.Models;
using backend.Library.Services.DataProviders;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Library.Services.EventFinalizers
{
    public class Mission1Finalizer : EventFinalizerBase<float>
    {
        public Mission1Finalizer(
            [FromKeyedServices(ServiceKeys.Mission1Extractor)] IDataProvider<float> provider
        )
            : base(provider) { }

        protected override EventData<(string, object)> Process(EventData<float> data)
        {
            return new EventData<(string, object)>
            {
                DataStamp = data.DataStamp,
                Data = ("mission1", data.Data),
            };
        }
    }
}
