using backend24.Models;

namespace backend24.Services.DataProviders
{
	/// <summary>
	/// Contract for a data provider.
	/// </summary>
	/// <typeparam name="T">The type of the data provided</typeparam>
	public interface IDataProvider<T>
	{
		/// <summary>
		/// Event triggered whenever the provider has new data.
		/// Subscribe to this event to act on the new data.
		/// </summary>
		public event Action<EventData<T>>? OnDataProvided;
	}
}
