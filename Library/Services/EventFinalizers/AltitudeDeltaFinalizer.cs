﻿using backend.Library.Models;
using backend.Library.Services;
using backend.Library.Services.DataProviders;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Library.Services.EventFinalizers
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
