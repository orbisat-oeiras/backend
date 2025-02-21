using backend.Library.Services.DataProcessors;
using backend.Library.Services.DataProviders;

namespace backend.Library.Services.EventFinalizers
{
    /// <summary>
    /// Base class for event finalizers. An event finalizer must inherit this class
    /// and must add a tag to incoming data (along with any processing deemed necessary).
    /// </summary>
    /// <typeparam name="T">Type of incoming data</typeparam>
    [EventFinalizer]
    public abstract class EventFinalizerBase<T> : DataProcessorBase<T, (string, object)> , IFinalizedProvider
    {
        protected EventFinalizerBase(IDataProvider<T> provider)
            : base(provider) { }
    }

    /// <summary>
    /// Newtype for the IDataProvider specialization implemented by a finalizer
    /// </summary>
    public interface IFinalizedProvider : IDataProvider<(string tag, object content)>
    {
    }
}
