using backend24.Services.DataProviders;

namespace backend24.Services.DataProcessors.DataExtractors
{
	/// <summary>
	/// Computes altitude from pressure and temperature data
	/// </summary>
	public class AltitudeExtractor : DataExtractorBase<float>
	{
		public AltitudeExtractor([FromKeyedServices(ServiceKeys.SerialProvider)] IDataProvider<Dictionary<SerialProvider.DataLabel, string>> provider) : base(provider) {
			_sourceIndexes = [SerialProvider.DataLabel.Pressure, SerialProvider.DataLabel.Temperature];
		}

		protected override float Convert(IEnumerable<string> data) {
			float pressure = float.Parse(data.First());
			// Add 273.15 to convert from Celsius to kelvin
			float temperature = float.Parse(data.Last()) + 273.15f;

			// Calculate the altitude from pressure and temperature. Based on the first formula from
			// https://physics.stackexchange.com/questions/333475/how-to-calculate-altitude-from-current-temperature-and-pressure
			// Physical constants are declared as variables in favour of readability and future changes to values.
			// Their values are obtained from https://en.wikipedia.org/wiki/Barometric_formula. For layer-varying values,
			// b=0 is used, as it appears to range between 0 and 11000 meters, so our maximum altitude of 1000 meters sits
			// comfortably under the threshold.
			float pressureRef = 101325f;
			// This represents the exponent (g0 * M)/(R * L)
			float exp = 5.2558f;
			float lapseRate = 0.0065f;
			return temperature * (MathF.Pow(pressureRef / pressure, 1 / exp) - 1) / lapseRate;
		}
	}
}
