namespace Alnudaar_ChildControlApp.Models
{
    public class Geofencing
    {
        public int GeofencingID { get; set; }
        public int UserID { get; set; }
        public string? SafeZoneName { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Radius { get; set; } // in meters

    }
}