namespace backend.Library.Models
{
    /// <summary>
    /// Reference data which is attached to all data.
    /// </summary>
    public readonly struct DataStamp
    {
        /// <summary>
        /// Nanosseconds since the Unix Epoch (00:00:00 UTC+0 1 January 1970).
        /// </summary>
        public ulong Timestamp { get; init; }

        /// <summary>
        /// Coordinates registered by the GPS when the data was sent
        /// </summary>
        public GPSCoords Coordinates { get; init; }
    }
}
