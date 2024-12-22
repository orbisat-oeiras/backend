using backend24.Models;
using backend24.Services.DataProviders;

namespace backend24.Services.DataProcessors
{
    /// <summary>
    /// Abstract base class for a data processor.
    /// Subscribes to an IDataProvider<T1>, transforms the T1 it provides into a T2, and re-sends the event
    /// </summary>
    /// <typeparam name="T1">The data type received by the processor</typeparam>
    /// <typeparam name="T2">The data type emitted by the processor</typeparam>
    public abstract class DataProcessorBase<T1, T2> : IDataProvider<T2>
    {
        public event Action<EventData<T2>>? OnDataProvided;

        /// <summary>
        /// Transform data from <see cref="T1"/> into <see cref="T2"/>
        /// </summary>
        /// <param name="data">The data to be processed</param>
        /// <returns>The processed data</returns>
        protected abstract EventData<T2> Process(EventData<T1> data);

        /// <summary>
        /// Re-sends the data that is received, after processing it.
        /// </summary>
        /// <param name="data">The data to be processed and re-sent</param>
        void BubbleUp(EventData<T1> data)
        {
            OnDataProvided?.Invoke(Process(data));
        }

        public DataProcessorBase(IDataProvider<T1> provider)
        {
            // Subscribe to a provider
            provider.OnDataProvided += BubbleUp;
        }
    }
}
