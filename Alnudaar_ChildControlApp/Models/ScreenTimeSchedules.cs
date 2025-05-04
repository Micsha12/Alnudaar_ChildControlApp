namespace Alnudaar_ChildControlApp.Models
{
    public class ScreenTimeSchedule
    {
        public int ScreenTimeScheduleID { get; set; }
        public int UserID { get; set; }
        public int? DeviceID { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public string? DayOfWeek { get; set; }

        // Navigation properties
        public User? User { get; set; }
        public Device? Device { get; set; }
    }
}