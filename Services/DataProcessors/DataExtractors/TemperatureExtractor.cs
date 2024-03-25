using backend24.Services.DataProviders;

namespace backend24.Services.DataProcessors.DataExtractors
{
	/// <summary>
	/// Extracts temperature data from SerialProvider data.
	/// </summary>
	public class TemperatureExtractor : DataExtractorBase<float>
	{
		public TemperatureExtractor([FromKeyedServices(ServiceKeys.SerialProvider)] IDataProvider<string[]> provider) : base(provider) {
			// Extract temperature data
			_sourceIndexes = [SerialProvider.DataIndexes.Temperature];
		}

		protected override float Convert(IEnumerable<string> data) => float.Parse(data.First());
	}
}
