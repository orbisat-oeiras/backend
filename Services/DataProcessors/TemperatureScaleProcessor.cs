using backend24.Models;
using backend24.Services.DataProviders;

namespace backend24.Services.DataProcessors
{
	/// <summary>
	/// Re-scales temperature, received in a [0;1] range, to an appropriate range.
	/// This class assumes received data is valid.
	/// </summary>
	public class TemperatureScaleProcessor : DataProcessorBase<float, float>
	{
		private readonly float _minTemp;
		private readonly float _maxTemp;

		/// <summary>
		/// Creates a new instance of TemperatureScaleProcessor.
		/// </summary>
		/// <param name="provider">Raw temperature data provider, see <seealso cref="DataProcessorBase{T1, T2}.DataProcessorBase(IDataProvider{T1})"/></param>
		/// <param name="minTemp">The minimum registered temperature, corresponds to 0 in the raw range</param>
		/// <param name="maxTemp">The maximum registered temperature, corresponds to 1 in the raw range</param>
		public TemperatureScaleProcessor(IDataProvider<float> provider, float minTemp, float maxTemp) : base(provider) {
			_minTemp = minTemp;
			_maxTemp = maxTemp;
		}

		protected override EventData<float> Process(EventData<float> data) {
			return new EventData<float> {
				DataStamp = data.DataStamp,
				Data = RescaleTemperature(data.Data)
			};
		}

		/// <summary>
		/// Rescale the input temperature.
		/// </summary>
		/// <param name="temperature">Raw temperature, in the range [0;1]</param>
		/// <returns>Scaled temperature, in the range [_minTemp;_maxTemp]</returns>
		private float RescaleTemperature(float temperature) => _minTemp + temperature * (_maxTemp - _minTemp);
	}
}
