using backend.Library.Models;
using backend.Library.Services.DataProviders;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Library.Services.DataProcessors.DataExtractors
{
    /// <summary>
    /// Base class for data extractors, i.e., classes which extract individual pieces of data from a SerialProvider
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class DataExtractorBase<T>
        : DataProcessorBase<Dictionary<SerialProvider.DataLabel, byte[]>, T>
    {
        /// <summary>
        /// Indexes of the required data pieces in the array provided by SerialProvider
        /// </summary>
        protected SerialProvider.DataLabel[] _sourceIndexes = [];

        protected DataExtractorBase(
            [FromKeyedServices(ServiceKeys.DataProvider)]
                IDataProvider<Dictionary<SerialProvider.DataLabel, byte[]>> provider
        )
            : base(provider) { }

        /// <summary>
        /// Convert the data piece into the proper format
        /// </summary>
        /// <param name="data">Data piece as a string</param>
        /// <returns>Data piece as <typeparamref name="T"/></returns>
        protected abstract T Convert(IEnumerable<byte[]> data);

        protected override EventData<T> Process(
            EventData<Dictionary<SerialProvider.DataLabel, byte[]>> data
        )
        {
            IEnumerable<byte[]> selectedData = _sourceIndexes.Select(x =>
                data.Data.TryGetValue(x, out byte[]? value) ? value : BitConverter.GetBytes(0.0f)
            );

            return new EventData<T> { DataStamp = data.DataStamp, Data = Convert(selectedData) };
        }
    }
}
