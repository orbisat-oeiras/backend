using backend24.Services.DataProcessors;
using backend24.Services.DataProviders;

namespace backend24.Services.EventFinalizers
{
	/// <summary>
	/// Base class for event finalizers. An event finalizer must inherit this class
	/// and must add a tag to incoming data (along with any processing deemed necessary).
	/// </summary>
	/// <typeparam name="T"></typeparam>
	[EventFinalizer]
	public abstract class EventFinalizerBase<T> : DataProcessorBase<T, (string, T)>
	{
		protected EventFinalizerBase(IDataProvider<T> provider) : base(provider) { }
	}
}
