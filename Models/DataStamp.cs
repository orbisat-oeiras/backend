namespace backend24.Models
{
	public readonly struct DataStamp
	{
		public long Timestamp { get; init; }
		public GPSCoords Coordinates { get; init; }
	}
}
