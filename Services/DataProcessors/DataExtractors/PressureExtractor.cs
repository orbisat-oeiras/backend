using backend24.Services.DataProviders;

namespace backend24.Services.DataProcessors.DataExtractors
{
	public class PressureExtractor : DataExtractorBase<float>
	{
		public PressureExtractor([FromKeyedServices(ServiceKeys.SerialProvider)] IDataProvider<string[]> provider) : base(provider) {
			sourceIndex = SerialProvider.DataIndexes.Pressure;
		}

		protected override float Convert(string data) => float.Parse(data);
	}
}
