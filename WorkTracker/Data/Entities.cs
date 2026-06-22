using System;
using System.ComponentModel.DataAnnotations;

namespace WorkTracker.Data
{
    public class AppUsageLog
    {
        public int Id { get; set; }
        
        [Required]
        public string ProcessName { get; set; } = string.Empty;
        
        public string WindowTitle { get; set; } = string.Empty;
        
        public DateTime StartTime { get; set; }
        
        public DateTime EndTime { get; set; }
        
        [Required]
        public string Category { get; set; } = "Work"; // Work, Leisure, Meeting, Idle, Offline Work
    }

    public class AppCategory
    {
        [Key]
        public string ProcessName { get; set; } = string.Empty;
        
        [Required]
        public string CategoryName { get; set; } = "Work"; // Work, Leisure, Meeting, Ignore
    }

    public class TimeOffLog
    {
        [Key]
        public DateTime Date { get; set; } // The day off (e.g. 2026-06-21 00:00:00)
        
        [Required]
        public string Type { get; set; } = "Vacation"; // Vacation, Sick Day, Public Holiday

        public double Hours { get; set; } = 8.0; // Custom hours credit (default to 8.0)
    }

    public class HolidayLog
    {
        public int Id { get; set; }
        
        [Required]
        public DateTime StartDate { get; set; }
        
        [Required]
        public DateTime EndDate { get; set; }
        
        public string Note { get; set; } = string.Empty;

        public bool IsPublicHoliday { get; set; }
    }

    /// <summary>Simple key-value store for app-wide settings (e.g. ICS calendar URL).</summary>
    public class AppSetting
    {
        [Key]
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
