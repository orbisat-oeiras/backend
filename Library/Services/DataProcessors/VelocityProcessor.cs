﻿using backend.Library.Models;
using backend.Library.Services;
using backend.Library.Services.DataProviders;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Library.Services.DataProcessors
{
    public class VelocityProcessor : DataProcessorBase<float, float>
    {
        private float _last = float.NaN;

        public VelocityProcessor(
            [FromKeyedServices(ServiceKeys.AltitudeExtractor)] IDataProvider<float> provider
        )
            : base(provider) { }

        protected override EventData<float> Process(EventData<float> data)
        {
            float vel = 0;
            if (!float.IsNaN(_last))
                vel = data.Data - _last;
            _last = data.Data;
            return data with { Data = vel };
        }
    }
}
