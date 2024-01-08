using System.ComponentModel.DataAnnotations;

namespace backend24.Models
{
	public struct GPSCoords
	{
		[Range(-90,90)]
        public float Latitude { get; set; }
		[Range(-180, 180)]
		public float Longitude { get; set; }
		public float Altitude { get; set; }
	}
}
