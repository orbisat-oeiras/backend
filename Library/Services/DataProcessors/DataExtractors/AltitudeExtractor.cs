using backend.Library.Services.DataProviders;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Library.Services.DataProcessors.DataExtractors
{
    /// <summary>
    /// Computes altitude from pressure and temperature data
    /// </summary>
    public class AltitudeExtractor : DataExtractorBase<float>
    {
        public AltitudeExtractor(
            [FromKeyedServices(ServiceKeys.DataProvider)]
                IDataProvider<Dictionary<SerialProvider.DataLabel, byte[]>> provider
        )
            : base(provider)
        {
            _sourceIndexes =
            [
                SerialProvider.DataLabel.Pressure,
                SerialProvider.DataLabel.Temperature,
                SerialProvider.DataLabel.Humidity,
            ];
        }

        protected override float Convert(IEnumerable<byte[]> data)
        {
            float pressure = BitConverter.ToSingle(data.First(), 0);
            // The temperature is stored in the second byte array, so we need to skip the first one.
            float temperature = BitConverter.ToSingle(data.ElementAt(1), 0) + 273.15f;
            float humidity = BitConverter.ToSingle(data.ElementAt(2), 0);

            // Calculate the altitude from pressure and temperature. Based on the first formula from
            // https://physics.stackexchange.com/questions/333475/how-to-calculate-altitude-from-current-temperature-and-pressure
            // Physical constants are declared as variables in favour of readability and future changes to values.
            // Their values are obtained from https://en.wikipedia.org/wiki/Barometric_formula. For layer-varying values,
            // b=0 is used, as it appears to range between 0 and 11000 meters, so our maximum altitude of 1000 meters sits
            // comfortably under the threshold.
            float pressureRef = 101325f;
            // This represents the exponent (g0 * M)/(R * L)

            float exp = 5.2558f;
            float e_s =
                6.122f
                * MathF.Pow(MathF.E, 17.67f * temperature / (temperature - 273.15f + 243.5f));
            float e = e_s * humidity / 100;
            float r = 0.622f * (e / (pressure - e));
            float lapseRate =
                9.8067f
                * (
                    (1 + 2501000f * r / (287.053f * temperature))
                    / (1003.5f + 2501000f * 2501000f * r / (461.52f * temperature * temperature))
                );
            float altitude =
                temperature
                / lapseRate
                * MathF.Pow(
                    101325f / pressure,
                    8.31432f * 1e3f * lapseRate / (9.8067f * 28.9644f) - 1
                );
            float pressure_bar_form = 44330 * (1 - MathF.Pow(pressure / 101325f, 1 / 5.255f));
            float corrected_alt = pressure_bar_form * (temperature / (15f + 273.15f));

            return altitude / 1e3f;
        }
    }
}
