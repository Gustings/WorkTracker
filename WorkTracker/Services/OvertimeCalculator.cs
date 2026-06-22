using System;
using System.Collections.Generic;
using System.Linq;
using WorkTracker.Data;

namespace WorkTracker.Services
{
    public class WeeklyOvertimeResult
    {
        public double TargetWorkHours { get; set; } = 40.0;
        public double WeekdayHoursWorked { get; set; }
        public double WeekendHoursWorked { get; set; }
        public double TimeOffHoursCredit { get; set; }
        public double NetWeekdayOvertimeHours { get; set; }
        
        // Total equivalent time off earned (after applying multipliers)
        public double TotalOvertimeTimeOffEarned { get; set; } 
        
        // Gross breakdown of overtime worked (before deficit offset)
        public double GrossOvertime10Hours { get; set; }
        public double GrossOvertime15Hours { get; set; }
        public double GrossOvertime20Hours { get; set; }
    }

    public static class OvertimeCalculator
    {
        public static bool IsUnitTest { get; set; } = false;

        private const double StandardDayHours = 8.0;
        private const double StandardWeekHours = 40.0;

        private class OvertimeSegment
        {
            public double DurationHours { get; set; }
            public double Multiplier { get; set; }
        }

        public static WeeklyOvertimeResult Calculate(
            DateTime weekStart,
            List<AppUsageLog> logs,
            List<TimeOffLog> timeOffLogs)
        {
            // Set start and end of week (Monday to Sunday)
            weekStart = GetStartOfWeek(weekStart);
            DateTime weekEnd = weekStart.AddDays(7);

            // Filter logs to this week first, apply rounding, and filter to work categories.
            // Exclude logs in the future (StartTime > DateTime.Now) so they are not counted before they happen.
            var cutoff = IsUnitTest ? DateTime.MaxValue : DateTime.Now;
            var weekLogs = logs.Where(l => l.StartTime >= weekStart && l.StartTime < weekEnd && l.StartTime <= cutoff).ToList();
            var roundedWeekLogs = ApplyRounding(weekLogs);

            var activeLogs = roundedWeekLogs
                .Where(l => l.StartTime >= weekStart && l.EndTime <= weekEnd)
                .Where(l => IsWorkCategory(l.Category) && l.ProcessName != "sick_time")
                .ToList();

            // Filter time off logs to this week (Mon-Fri only for standard target deduction)
            var weekTimeOffs = timeOffLogs
                .Where(t => t.Date >= weekStart && t.Date < weekStart.AddDays(5))
                .ToList();

            // Also count sick-day AppUsageLogs (Mon-Fri only) as target reduction
            double sickDayHours = 0;
            for (int s = 0; s < 5; s++)
            {
                DateTime sDayStart = weekStart.AddDays(s);
                DateTime sDayEnd   = sDayStart.AddDays(1);
                sickDayHours += weekLogs
                    .Where(l => l.ProcessName == "sick_time" &&
                                l.StartTime >= sDayStart && l.StartTime < sDayEnd)
                    .Sum(l => (l.EndTime - l.StartTime).TotalHours);
            }

            double timeOffHours = weekTimeOffs.Sum(t => t.Hours) + sickDayHours;
            double targetHours  = Math.Max(0, StandardWeekHours - timeOffHours);


            double weekdayHours = 0;
            double weekendHours = 0;

            var weekdayOvertimeSegments = new List<OvertimeSegment>();

            // Group logs by day of week (Monday = 0 ... Sunday = 6)
            for (int i = 0; i < 7; i++)
            {
                DateTime dayStart = weekStart.AddDays(i);
                DateTime dayEnd = dayStart.AddDays(1);

                var dayLogs = activeLogs
                    .Where(l => l.StartTime >= dayStart && l.StartTime < dayEnd)
                    .ToList();

                var mergedDayLogs = MergeOverlappingLogs(dayLogs);

                double totalDayHours = mergedDayLogs.Sum(l => (l.EndTime - l.StartTime).TotalHours);

                if (i < 5) // Monday to Friday
                {
                    weekdayHours += totalDayHours;

                    // Calculate daily overtime segments strictly by time of day
                    foreach (var log in mergedDayLogs)
                    {
                        var segments = SplitOvertimeByTimeOfDay(log.StartTime, log.EndTime);
                        // Only add segments where the multiplier > 1.0 (overtime)
                        weekdayOvertimeSegments.AddRange(segments.Where(s => s.Multiplier > 1.0));
                    }
                }
                else // Saturday and Sunday
                {
                    // Weekend work is ALWAYS overtime at 2.0x multiplier
                    weekendHours += totalDayHours;
                }
            }

            double netWeekdayOvertimeHours = weekdayOvertimeSegments.Sum(s => s.DurationHours);
            double weekdayOvertimeTimeOffEarned = 0;

            // Group and summarize gross segments
            double gross10 = weekdayOvertimeSegments.Where(s => s.Multiplier == 1.0).Sum(s => s.DurationHours);
            double gross15 = weekdayOvertimeSegments.Where(s => s.Multiplier == 1.5).Sum(s => s.DurationHours);
            double gross20 = weekdayOvertimeSegments.Where(s => s.Multiplier == 2.0).Sum(s => s.DurationHours);

            foreach (var seg in weekdayOvertimeSegments)
            {
                weekdayOvertimeTimeOffEarned += seg.DurationHours * seg.Multiplier;
            }

            // Weekend overtime always counts at 2.0x
            double weekendOvertimeTimeOffEarned = weekendHours * 2.0;

            return new WeeklyOvertimeResult
            {
                TargetWorkHours = targetHours,
                WeekdayHoursWorked = weekdayHours,
                WeekendHoursWorked = weekendHours,
                TimeOffHoursCredit = timeOffHours,
                NetWeekdayOvertimeHours = netWeekdayOvertimeHours,
                TotalOvertimeTimeOffEarned = weekdayOvertimeTimeOffEarned + weekendOvertimeTimeOffEarned,
                GrossOvertime10Hours = gross10,
                GrossOvertime15Hours = gross15,
                GrossOvertime20Hours = gross20
            };
        }

        public static DateTime GetStartOfWeek(DateTime dt)
        {
            // Get Monday of the week containing dt
            int diff = (7 + (dt.DayOfWeek - DayOfWeek.Monday)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }

        public static bool IsWorkCategory(string category)
        {
            return category == "Work" || category == "Meeting" || category == "Offline Work";
        }

        public static List<AppUsageLog> ApplyRounding(List<AppUsageLog> logs)
        {
            if (logs == null || logs.Count == 0) return new List<AppUsageLog>();

            // Filter out any virtual rounded_time logs that were already added to the list (for safety)
            var cleanLogs = logs.Where(l => l.ProcessName != "rounded_time").ToList();
            var result = new List<AppUsageLog>(cleanLogs);

            var logsByDate = cleanLogs.GroupBy(l => l.StartTime.Date);

            foreach (var dateGroup in logsByDate)
            {
                DateTime dayStart = dateGroup.Key;
                var dayWorkLogs = dateGroup.Where(l => IsWorkCategory(l.Category)).ToList();
                if (dayWorkLogs.Count == 0) continue;

                double[] blockWorkSeconds = new double[48];

                for (int k = 0; k < 48; k++)
                {
                    DateTime blockStart = dayStart.AddMinutes(k * 30);
                    DateTime blockEnd = blockStart.AddMinutes(30);

                    double activeSec = 0;
                    foreach (var log in dayWorkLogs)
                    {
                        if (log.StartTime < blockEnd && log.EndTime > blockStart)
                        {
                            DateTime overlapStart = log.StartTime < blockStart ? blockStart : log.StartTime;
                            DateTime overlapEnd = log.EndTime > blockEnd ? blockEnd : log.EndTime;
                            double overlapSec = (overlapEnd - overlapStart).TotalSeconds;
                            if (overlapSec > 0)
                            {
                                activeSec += overlapSec;
                            }
                        }
                    }
                    blockWorkSeconds[k] = activeSec;
                }

                for (int k = 0; k < 48; k++)
                {
                    double W_k = blockWorkSeconds[k];
                    double W_k_next = 0;

                    if (k < 47)
                    {
                        W_k_next = blockWorkSeconds[k + 1];
                    }
                    else
                    {
                        // Check first block of next day
                        DateTime nextDayStart = dayStart.AddDays(1);
                        DateTime nextDayBlockEnd = nextDayStart.AddMinutes(30);
                        W_k_next = logs
                            .Where(l => IsWorkCategory(l.Category) && l.StartTime < nextDayBlockEnd && l.EndTime > nextDayStart)
                            .Sum(l => {
                                DateTime overlapStart = l.StartTime < nextDayStart ? nextDayStart : l.StartTime;
                                DateTime overlapEnd = l.EndTime > nextDayBlockEnd ? nextDayBlockEnd : l.EndTime;
                                return Math.Max(0, (overlapEnd - overlapStart).TotalSeconds);
                            });
                    }

                    // Rule: If worked 20 mins or more (1200s), but less than 30 mins (1800s),
                    // AND work continues in the next block (> 30s)
                    if (W_k >= 1200 && W_k < 1800 && W_k_next > 30)
                    {
                        double secondsToAdd = 1800 - W_k;
                        DateTime blockStart = dayStart.AddMinutes(k * 30);
                        
                        result.Add(new AppUsageLog
                        {
                            ProcessName = "rounded_time",
                            WindowTitle = "Timeline Rounding Adjustment",
                            StartTime = blockStart,
                            EndTime = blockStart.AddSeconds(secondsToAdd),
                            Category = "Work"
                        });
                    }
                }
            }

            return result;
        }

        private static List<OvertimeSegment> SplitOvertimeByTimeOfDay(DateTime start, DateTime end)
        {
            var segments = new List<OvertimeSegment>();
            DateTime current = start;

            while (current < end)
            {
                // Determine the next boundary or the end of the segment
                DateTime nextBoundary = GetNextMultiplierBoundary(current, end);
                double durationHours = (nextBoundary - current).TotalHours;
                double multiplier = GetMultiplierForTime(current);

                segments.Add(new OvertimeSegment
                {
                    DurationHours = durationHours,
                    Multiplier = multiplier
                });

                current = nextBoundary;
            }

            return segments;
        }

        private static DateTime GetNextMultiplierBoundary(DateTime current, DateTime limit)
        {
            // Multiplier boundaries are at 08:00, 16:00, and 21:00
            DateTime b8 = current.Date.AddHours(8);
            DateTime b16 = current.Date.AddHours(16);
            DateTime b21 = current.Date.AddHours(21);
            DateTime bNext8 = current.Date.AddDays(1).AddHours(8);

            DateTime next = limit;

            if (current < b8 && b8 < next) next = b8;
            if (current < b16 && b16 < next) next = b16;
            if (current < b21 && b21 < next) next = b21;
            if (current < bNext8 && bNext8 < next) next = bNext8;

            return next;
        }

        private static double GetMultiplierForTime(DateTime time)
        {
            double hour = time.Hour + time.Minute / 60.0;

            // 16:00 - 21:00 => 1.5x
            if (hour >= 16.0 && hour < 21.0)
            {
                return 1.5;
            }
            // 21:00 - 08:00 => 2.0x
            if (hour >= 21.0 || hour < 8.0)
            {
                return 2.0;
            }
            // 08:00 - 16:00 => 1.0x
            return 1.0;
        }

        public static List<AppUsageLog> MergeOverlappingLogs(List<AppUsageLog> logs)
        {
            if (logs == null || logs.Count == 0) return new List<AppUsageLog>();

            var sorted = logs.OrderBy(l => l.StartTime).ToList();
            var merged = new List<AppUsageLog>();

            var current = new AppUsageLog
            {
                ProcessName = sorted[0].ProcessName,
                WindowTitle = sorted[0].WindowTitle,
                StartTime = sorted[0].StartTime,
                EndTime = sorted[0].EndTime,
                Category = sorted[0].Category
            };

            for (int i = 1; i < sorted.Count; i++)
            {
                var next = sorted[i];
                if (next.StartTime <= current.EndTime)
                {
                    if (next.EndTime > current.EndTime)
                    {
                        current.EndTime = next.EndTime;
                    }
                }
                else
                {
                    merged.Add(current);
                    current = new AppUsageLog
                    {
                        ProcessName = next.ProcessName,
                        WindowTitle = next.WindowTitle,
                        StartTime = next.StartTime,
                        EndTime = next.EndTime,
                        Category = next.Category
                    };
                }
            }
            merged.Add(current);
            return merged;
        }
    }
}
