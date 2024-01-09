namespace backend24.Models
{
	public readonly struct EventData<T>
	{
        public DataStamp DataStamp { get; init; }
        public T Data { get; init; }
    }
}
