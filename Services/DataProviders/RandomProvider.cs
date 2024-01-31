using backend24.Models;

namespace backend24.Services.DataProviders
{
	/// <summary>
	/// Provides a random float on a configurable interval.
	/// </summary>
	public class RandomProvider : IDataProvider<float>
	{
		public event Action<EventData<float>>? OnDataProvided;

		private readonly Random _random;
		private System.Timers.Timer? _timer;
		private int _intervalMs;
		private long _elapsedMs;

		/// <summary>
		/// Initialises a new instance of RandomProvider.
		/// </summary>
		/// <param name="random">A random number generator to be used internally</param>
		public RandomProvider(Random random) {
			_random = random;
		}

		/// <summary>
		/// Starts the provider.
		/// </summary>
		/// <remarks>
		/// The provider will periodically raise an event with random data
		/// until it is stopped.
		/// </remarks>
		/// <param name="intervalMs">The time, in milliseconds, between data sends</param>
		public void Start(int intervalMs) {
			_intervalMs = intervalMs;
			_timer = new System.Timers.Timer(_intervalMs);
			_timer.Elapsed += (s, e) =>
			{
				OnDataProvided?.Invoke(GetRandomData());
				_elapsedMs += _intervalMs;
			};
			_timer?.Start();
		}

		/// <summary>
		/// Creates a new instance of EventData<float> initialized with random data.
		/// </summary>
		/// <returns>An instance of EventData<float> initialized with random data</returns>
		private EventData<float> GetRandomData(){
			return new EventData<float>
			{
				DataStamp = new DataStamp
				{
					Timestamp = _elapsedMs,
					Coordinates = new GPSCoords
					{
						// Map the values from [0;1] to the valid range
						Latitude = _random.NextSingle() * 180 - 90,
						Longitude = _random.NextSingle() * 360 - 180,
						Altitude = _random.NextSingle() * 1000
					}
				},
				Data = _random.NextSingle()
			};
		}
		/// <summary>
		/// Stops the provider.
		/// </summary>
		public void Stop() => _timer?.Stop();
	}
}
