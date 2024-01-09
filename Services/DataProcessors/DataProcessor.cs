using backend24.Models;
using backend24.Services.ServerEventSenders;

using Microsoft.AspNetCore.Mvc.Diagnostics;

namespace backend24.Services.DataProcessors
{
	/// <summary>
	/// Subscribes to an IDataProvider<T1>, transforms the T1 it provides into a T2, and re-sends the event
	/// </summary>
	/// <typeparam name="T1">The data type received by the processor</typeparam>
	/// <typeparam name="T2">The data type emitted by the processor</typeparam>
	public abstract class DataProcessor<T1,T2> : IDataProvider<T2>
	{
		public event Action<EventData<T2>>? OnDataProvided;

		protected abstract EventData<T2> Process(EventData<T1> data);
		void BubbleUp(EventData<T1> data) {
			OnDataProvided?.Invoke(Process(data));
		}

		public DataProcessor(IDataProvider<T1> provider) {
			provider.OnDataProvided += BubbleUp;
		}
	}
}
