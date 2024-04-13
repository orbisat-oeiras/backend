namespace backend24.Models
{
	/// <summary>
	/// Reference data which is attached to all data.
	/// </summary>
	public readonly struct DataStamp
	{
		/// <summary>
		/// Milliseconds since the Arduino was initialized, registered
		/// when the data was sent.
		/// </summary>
		public long Timestamp { get; init; }
		/// <summary>
		/// Coordinates registered by the GPS when the data was sent
		/// </summary>
		public GPSCoords Coordinates { get; init; }
	}
}
