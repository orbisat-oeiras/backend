using System.ComponentModel.DataAnnotations;

namespace backend24.Models
{
	/// <summary>
	/// Represents GPS coordinates - latitude, longitude and altitude.
	/// </summary>
	public readonly struct GPSCoords
	{
		/// <summary>
		/// Vertical angle, measured with respoect to the equator.
		/// -90º is the south pole, 0º is the equator and 90º is the north pole.
		/// </summary>
		[Range(-90,90)]
        public float Latitude { get; init; }
		/// <summary>
		/// Horizontal angle, measured with respect to the Greenwich semi-meridian.
		/// 0º is the Greenwich semi-meridian, 180º and -180º
		/// </summary>
		[Range(-180, 180)]
		public float Longitude { get; init; }
		/// <summary>
		/// Meters above average sea level.
		/// </summary>
		public float Altitude { get; init; }
	}
}
