namespace backend24.Services.EventFinalizers
{
    /// <summary>
    /// Marks a class as an event finalizer, telling ServerEventsController to subscribe to it.
    /// A class shouldn't use this attribute directly, but instead inherit EventFinalizerBase.
    /// </summary>
    /// <remarks>
    /// The compiler allows classes besides EventFinalizerBase to use this attribute,
    /// which may result in runtime errors.
    /// TODO: find a way to improve this, perhaps with a code analyser.
    /// TODO: I think this is useless...
    /// </remarks>
    [System.AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    internal sealed class EventFinalizerAttribute : Attribute { }
}
