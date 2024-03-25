using backend24.Models;
using backend24.Services.DataProviders;

namespace backend24.Services.DataProcessors.DataExtractors
{
	/// <summary>
	/// Base class for data extractors, i.e., classes which extract individual pieces of data from a SerialProvider
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class DataExtractorBase<T> : DataProcessorBase<string[], T>
	{
		/// <summary>
		/// Index of the required data piece in the array provided by SerialProvider
		/// </summary>
		protected SerialProvider.DataIndexes _sourceIndex;
		protected DataExtractorBase([FromKeyedServices(ServiceKeys.SerialProvider)]IDataProvider<string[]> provider) : base(provider) {
		}

		/// <summary>
		/// Convert the data piece into the proper format
		/// </summary>
		/// <param name="data">Data piece as a string</param>
		/// <returns>Data piece as <typeparamref name="T"/></returns>
		protected abstract T Convert(string data);
		protected override EventData<T> Process(EventData<string[]> data) {
			return new EventData<T>() {
				DataStamp = data.DataStamp,
				Data = Convert(data.Data[(int)_sourceIndex])
			};
		}
	}
}
