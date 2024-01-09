using backend24.Models;

namespace backend24.Services.ServerEventSenders
{
	public interface IDataProvider<T>
	{
		public event Action<EventData<T>>? OnDataProvided;
	}
}
