namespace Alnudaar_ChildControlApp.Models
{
    public class Alert
    {
        public int AlertID { get; set; }
        public int UserID { get; set; }
        public int DeviceID { get; set; }
        public string? AlertType { get; set; }
        public DateTime AlertTime { get; set; }
        public string? Details { get; set; }

        // Navigation properties
        public Device? Device { get; set; }
    }
}