// Type alias for the IDataProvider specialization implemented by a finalizer
global using IFinalizedProvider = backend24.Services.DataProviders.IDataProvider<(
    string tag,
    object content
)>;
using backend24.Services.DataProcessors;
using backend24.Services.DataProviders;

namespace backend24.Services.EventFinalizers
{
    /// <summary>
    /// Base class for event finalizers. An event finalizer must inherit this class
    /// and must add a tag to incoming data (along with any processing deemed necessary).
    /// </summary>
    /// <typeparam name="T">Type of incoming data</typeparam>
    [EventFinalizer]
    public abstract class EventFinalizerBase<T> : DataProcessorBase<T, (string, object)>
    {
        protected EventFinalizerBase(IDataProvider<T> provider)
            : base(provider) { }
    }
}
