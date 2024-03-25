using backend24.Services.DataProviders;

namespace backend24.Services.DataProcessors.DataExtractors
{
	/// <summary>
	/// Extracts pressure data from SerialProvider data.
	/// </summary>
	public class PressureExtractor : DataExtractorBase<float>
	{
		public PressureExtractor([FromKeyedServices(ServiceKeys.SerialProvider)] IDataProvider<string[]> provider) : base(provider) {
			// Extract pressure data
			_sourceIndex = SerialProvider.DataIndexes.Pressure;
		}

		protected override float Convert(string data) => float.Parse(data);
	}
}
