using backend24.Services.DataProviders;

namespace backend24.Services.DataProcessors.DataExtractors
{
	public class TemperatureExtractor : DataExtractorBase<float>
	{
		public TemperatureExtractor([FromKeyedServices(ServiceKeys.SerialProvider)] IDataProvider<string[]> provider) : base(provider) {
			sourceIndex = SerialProvider.DataIndexes.Temperature;
		}

		protected override float Convert(string data) => float.Parse(data);
	}
}
