using backend.Library.Models;
using backend.Library.Services;
using backend.Library.Services.DataProcessors;
using backend.Library.Services.DataProviders;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Library.Services.DataProcessors.DataExtractors
{
    /// <summary>
    /// Base class for data extractors, i.e., classes which extract individual pieces of data from a SerialProvider
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class DataExtractorBase<T>
        : DataProcessorBase<Dictionary<LegacySerialProvider.DataLabel, string>, T>
    {
        /// <summary>
        /// Indexes of the required data pieces in the array provided by SerialProvider
        /// </summary>
        protected LegacySerialProvider.DataLabel[] _sourceIndexes = [];

        protected DataExtractorBase(
            [FromKeyedServices(ServiceKeys.DataProvider)]
                IDataProvider<Dictionary<LegacySerialProvider.DataLabel, string>> provider
        )
            : base(provider) { }

        /// <summary>
        /// Convert the data piece into the proper format
        /// </summary>
        /// <param name="data">Data piece as a string</param>
        /// <returns>Data piece as <typeparamref name="T"/></returns>
        protected abstract T Convert(IEnumerable<string> data);

        protected override EventData<T> Process(
            EventData<Dictionary<LegacySerialProvider.DataLabel, string>> data
        )
        {
            return new EventData<T>()
            {
                DataStamp = data.DataStamp,
                // Select a subset of the data pieces
                Data = Convert(_sourceIndexes.Select(x => data.Data[x])),
            };
        }
    }
}
