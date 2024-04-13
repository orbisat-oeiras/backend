namespace backend24.Models
{
    /// <summary>
    /// Wrapper class which packs the data along with a
    /// <see cref="Models.DataStamp"/> object
    /// </summary>
    /// <typeparam name="T"></typeparam>
	public readonly struct EventData<T>
	{
        public DataStamp DataStamp { get; init; }
        public T Data { get; init; }
    }
}
