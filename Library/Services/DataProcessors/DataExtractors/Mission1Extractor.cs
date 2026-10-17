using backend.Library.Models;
using backend.Library.Services.DataProviders;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Library.Services.DataProcessors.DataExtractors
{
    /// <summary>
    /// Extracts Acceleration X data from SerialProvider data.
    /// </summary>
    public class Mission1Extractor : DataExtractorBase<float>
    {
        public Mission1Extractor(
            [FromKeyedServices(ServiceKeys.DataProvider)]
                IDataProvider<Dictionary<SerialProvider.DataLabel, byte[]>> provider
        )
            : base(provider)
        {
            // Extract Acceleration X data
            _sourceIndexes = [SerialProvider.DataLabel.Mission1];
        }

        private float _lastStatus = 0f;

        protected override float Convert(IEnumerable<byte[]> data)
        {
            byte[] bytes = data.First();
            if (bytes == null || bytes.Length == 0) return 0f;
            if (bytes.Length == 1) return bytes[0];
            if (bytes.Length >= 4) return BitConverter.ToInt32(bytes, 0);
            return 0f;
        }

        protected override EventData<float> Process(
            EventData<Dictionary<SerialProvider.DataLabel, byte[]>> data
        )
        {
            if (data.Data.TryGetValue(SerialProvider.DataLabel.Mission1, out byte[]? value))
            {
                _lastStatus = Convert([value]);
            }

            return new EventData<float>
            {
                DataStamp = data.DataStamp,
                Data = _lastStatus,
            };
        }
    }
}
