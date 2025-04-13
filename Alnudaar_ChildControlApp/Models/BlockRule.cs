namespace Alnudaar_ChildControlApp.Models
{
    public class BlockRule
    {
        public int BlockRuleID { get; set; }
        public int UserID { get; set; }
        public required string Type { get; set; } // "website" or "application"
        public required string Value { get; set; } // Website URL or application name
        public required string TimeRange { get; set; } // e.g., "09:00-17:00"
        public int DeviceID { get; set; } // ID of the device this rule applies to
    }
}