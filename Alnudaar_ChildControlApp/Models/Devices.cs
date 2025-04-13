namespace Alnudaar_ChildControlApp.Models
{
    public class Device
    {
        public int DeviceID { get; set; }
        public string? Name { get; set; }
        public int UserID { get; set; }
        public ICollection<ActivityLog>? ActivityLogs { get; set; }
        public ICollection<Alert>? Alerts { get; set; }
        public ICollection<AppUsageReport>? AppUsageReports { get; set; }
        public ICollection<ScreenTimeSchedule>? ScreenTimeSchedules { get; set; }
    }
}