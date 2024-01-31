using backend24.Models;
using backend24.Services.DataProviders;

namespace backend24.Services.EventFinalizers
{
	/// <summary>
	/// Finalizes a tempreature event, tagged with "primary/temperature".
	/// </summary>
	public class TemperatureFinalizer : EventFinalizerBase<float>
	{
		public TemperatureFinalizer(IDataProvider<float> provider) : base(provider) { }

		protected override EventData<(string, float)> Process(EventData<float> data) {
			return new EventData<(string, float)> {
				DataStamp = data.DataStamp,
				Data = ("primary/temperature", data.Data),
			};
		}
	}
}
