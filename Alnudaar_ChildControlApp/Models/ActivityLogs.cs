namespace Alnudaar_ChildControlApp.Models
{
    public class ActivityLog
    {
        public int ActivityLogID { get; set; } // Primary key
        public int UserID { get; set; }
        public int DeviceID { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Activity { get; set; }

        // Navigation properties
        public Device? Device { get; set; }
    }
}