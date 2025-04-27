using System.Text.Json.Serialization;

namespace Alnudaar_ChildControlApp.Models
{
    public class Device
    {
        [JsonPropertyName("deviceId")]
        public int DeviceID { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("userId")]
        public int UserID { get; set; }

        [JsonPropertyName("user")]
        public User? User { get; set; }

        [JsonPropertyName("activityLogs")]
        public ICollection<ActivityLog>? ActivityLogs { get; set; }

        [JsonPropertyName("alerts")]
        public ICollection<Alert>? Alerts { get; set; }

        [JsonPropertyName("appUsageReports")]
        public ICollection<AppUsageReport>? AppUsageReports { get; set; }

        [JsonPropertyName("screenTimeSchedules")]
        public ICollection<ScreenTimeSchedule>? ScreenTimeSchedules { get; set; }
    }
}