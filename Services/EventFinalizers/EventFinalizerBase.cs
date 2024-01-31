using backend24.Services.DataProcessors;
using backend24.Services.DataProviders;

namespace backend24.Services.EventFinalizers
{
	[EventFinalizer]
	public abstract class EventFinalizerBase<T> : DataProcessorBase<T, (string, T)>
	{
		protected EventFinalizerBase(IDataProvider<T> provider) : base(provider) { }
	}
}
