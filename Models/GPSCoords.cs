using System.ComponentModel.DataAnnotations;

namespace backend24.Models
{
	public readonly struct GPSCoords
	{
		[Range(-90,90)]
        public float Latitude { get; init; }
		[Range(-180, 180)]
		public float Longitude { get; init; }
		public float Altitude { get; init; }
	}
}
