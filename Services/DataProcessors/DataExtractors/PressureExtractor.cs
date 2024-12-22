using backend24.Services.DataProviders;

namespace backend24.Services.DataProcessors.DataExtractors
{
    /// <summary>
    /// Extracts pressure data from SerialProvider data.
    /// </summary>
    public class PressureExtractor : DataExtractorBase<float>
    {
        public PressureExtractor(
            [FromKeyedServices(ServiceKeys.SerialProvider)]
                IDataProvider<Dictionary<SerialProvider.DataLabel, string>> provider
        )
            : base(provider)
        {
            // Extract pressure data
            _sourceIndexes = [SerialProvider.DataLabel.Pressure];
        }

        protected override float Convert(IEnumerable<string> data) => float.Parse(data.First());
    }
}
