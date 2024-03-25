using backend24.Models;
using backend24.Services.DataProviders;

namespace backend24.Services.DataProcessors.DataExtractors
{
	public abstract class DataExtractorBase<T> : DataProcessorBase<string[], T>
	{
		protected SerialProvider.DataIndexes sourceIndex;
		protected DataExtractorBase([FromKeyedServices(ServiceKeys.SerialProvider)]IDataProvider<string[]> provider) : base(provider) {
		}

		protected abstract T Convert(string data);
		protected override EventData<T> Process(EventData<string[]> data) {
			return new EventData<T>() {
				DataStamp = data.DataStamp,
				Data = Convert(data.Data[(int)sourceIndex])
			};
		}
	}
}
