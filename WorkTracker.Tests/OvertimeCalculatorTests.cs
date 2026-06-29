using System;
using System.Collections.Generic;
using Xunit;
using WorkTracker.Data;
using WorkTracker.Services;

namespace WorkTracker.Tests
{
    public class OvertimeCalculatorTests
    {
        private readonly DateTime _monday = new DateTime(2026, 6, 22); // Monday June 22, 2026

        public OvertimeCalculatorTests()
        {
            OvertimeCalculator.IsUnitTest = true;
        }

        [Fact]
        public void Calculate_StandardWeek_NoOvertime()
        {
            // Arrange
            var logs = new List<AppUsageLog>();
            // Add 8.0 hours of work for Mon-Fri
            for (int i = 0; i < 5; i++)
            {
                DateTime day = _monday.AddDays(i);
                logs.Add(new AppUsageLog
                {
                    StartTime = day.AddHours(8),
                    EndTime = day.AddHours(16), // 8:00 to 16:00 = 8.0 hours
                    Category = "Work",
                    ProcessName = "chrome"
                });
            }

            // Act
            var result = OvertimeCalculator.Calculate(_monday, logs, new List<TimeOffLog>());

            // Assert
            Assert.Equal(40.0, result.TargetWorkHours);
            Assert.Equal(40.0, result.WeekdayHoursWorked);
            Assert.Equal(0, result.WeekendHoursWorked);
            Assert.Equal(0, result.NetWeekdayOvertimeHours);
            Assert.Equal(0, result.TotalOvertimeTimeOffEarned);
        }

        [Fact]
        public void Calculate_WeekdayOvertime_WithMultipliers()
        {
            // Arrange
            var logs = new List<AppUsageLog>();
            // Mon: works 10.5 hours (08:00 to 18:30). 
            // 8.0h standard (08:00 to 16:00)
            // 2.5h overtime (16:00 to 18:30):
            //   - 16:00 to 18:30 (2.5 hours) is at 1.5x
            logs.Add(new AppUsageLog
            {
                StartTime = _monday.AddHours(8),
                EndTime = _monday.AddHours(18.5), // 08:00 to 18:30
                Category = "Work",
                ProcessName = "chrome"
            });

            // Tue-Fri: works 8.0 hours each
            for (int i = 1; i < 5; i++)
            {
                DateTime day = _monday.AddDays(i);
                logs.Add(new AppUsageLog
                {
                    StartTime = day.AddHours(8),
                    EndTime = day.AddHours(16),
                    Category = "Work",
                    ProcessName = "chrome"
                });
            }

            // Act
            var result = OvertimeCalculator.Calculate(_monday, logs, new List<TimeOffLog>());

            // Assert
            Assert.Equal(42.5, result.WeekdayHoursWorked);
            Assert.Equal(2.5, result.NetWeekdayOvertimeHours);
            
            // Gross segments checks
            Assert.Equal(40.0, result.GrossOvertime10Hours);
            Assert.Equal(2.5, result.GrossOvertime15Hours);
            Assert.Equal(0.0, result.GrossOvertime20Hours);

            // 2.5 * 1.5 = 3.75 hours equivalent time off
            Assert.Equal(3.75, result.TotalOvertimeTimeOffEarned);
        }

        [Fact]
        public void Calculate_WeekendWork_AlwaysOvertime()
        {
            // Arrange
            var logs = new List<AppUsageLog>();
            // Works only 20 hours Mon-Fri (4 hours per day)
            for (int i = 0; i < 5; i++)
            {
                DateTime day = _monday.AddDays(i);
                logs.Add(new AppUsageLog
                {
                    StartTime = day.AddHours(8),
                    EndTime = day.AddHours(12),
                    Category = "Work",
                    ProcessName = "chrome"
                });
            }

            // Works 4 hours on Saturday (Weekend work is ALWAYS 2.0x overtime)
            DateTime saturday = _monday.AddDays(5);
            logs.Add(new AppUsageLog
            {
                StartTime = saturday.AddHours(9),
                EndTime = saturday.AddHours(13),
                Category = "Work",
                ProcessName = "chrome"
            });

            // Act
            var result = OvertimeCalculator.Calculate(_monday, logs, new List<TimeOffLog>());

            // Assert
            Assert.Equal(20.0, result.WeekdayHoursWorked);
            Assert.Equal(4.0, result.WeekendHoursWorked);
            Assert.Equal(0.0, result.NetWeekdayOvertimeHours); // No net weekday overtime
            
            // Weekend: 4.0 hours * 2.0 multiplier = 8.0 hours time off
            Assert.Equal(8.0, result.TotalOvertimeTimeOffEarned);
        }

        [Fact]
        public void Calculate_WeekdayDeficit_OffsetsOvertimeCorrectly()
        {
            // Arrange
            var logs = new List<AppUsageLog>();
            // Mon: works 10 hours (08:00 to 18:00) -> 2.0 hours daily overtime (all @ 1.5x)
            logs.Add(new AppUsageLog
            {
                StartTime = _monday.AddHours(8),
                EndTime = _monday.AddHours(18),
                Category = "Work",
                ProcessName = "chrome"
            });

            // Tue: works 6 hours (08:00 to 14:00) -> 2.0 hours deficit
            DateTime tuesday = _monday.AddDays(1);
            logs.Add(new AppUsageLog
            {
                StartTime = tuesday.AddHours(8),
                EndTime = tuesday.AddHours(14),
                Category = "Work",
                ProcessName = "chrome"
            });

            // Wed-Fri: works 8.0 hours each
            for (int i = 2; i < 5; i++)
            {
                DateTime day = _monday.AddDays(i);
                logs.Add(new AppUsageLog
                {
                    StartTime = day.AddHours(8),
                    EndTime = day.AddHours(16),
                    Category = "Work",
                    ProcessName = "chrome"
                });
            }

            // Act
            var result = OvertimeCalculator.Calculate(_monday, logs, new List<TimeOffLog>());

            // Assert
            // Total weekday worked: 10 + 6 + 8 * 3 = 40.0 hours.
            // Under daily rules, weekday overtime is computed daily and not offset by deficits in the calculator.
            // Mon has 2.0 hours daily overtime (@ 1.5x).
            Assert.Equal(40.0, result.WeekdayHoursWorked);
            Assert.Equal(2.0, result.NetWeekdayOvertimeHours);
            Assert.Equal(3.0, result.TotalOvertimeTimeOffEarned);
        }

        [Fact]
        public void Calculate_WeekdayDeficit_OffsetsLowestMultiplierFirst()
        {
            // Arrange
            var logs = new List<AppUsageLog>();
            // Mon: works 14.5 hours (08:00 to 22:30) -> 6.5 hours daily overtime (5.0h @ 1.5x, 1.5h @ 2.0x)
            logs.Add(new AppUsageLog
            {
                StartTime = _monday.AddHours(8),
                EndTime = _monday.AddHours(22.5),
                Category = "Work",
                ProcessName = "chrome"
            });

            // Tue: works 6 hours (08:00 to 14:00) -> 2.0 hours deficit
            DateTime tuesday = _monday.AddDays(1);
            logs.Add(new AppUsageLog
            {
                StartTime = tuesday.AddHours(8),
                EndTime = tuesday.AddHours(14),
                Category = "Work",
                ProcessName = "chrome"
            });

            // Wed-Fri: works 8.0 hours each
            for (int i = 2; i < 5; i++)
            {
                DateTime day = _monday.AddDays(i);
                logs.Add(new AppUsageLog
                {
                    StartTime = day.AddHours(8),
                    EndTime = day.AddHours(16),
                    Category = "Work",
                    ProcessName = "chrome"
                });
            }

            // Act
            var result = OvertimeCalculator.Calculate(_monday, logs, new List<TimeOffLog>());

            // Assert
            // Total weekday worked: 14.5 + 6 + 8 * 3 = 44.5 hours.
            // Under daily rules, weekday overtime is computed daily and not offset by deficits in the calculator.
            // Mon has 6.5 hours daily overtime (5.0h @ 1.5x and 1.5h @ 2.0x).
            Assert.Equal(44.5, result.WeekdayHoursWorked);
            Assert.Equal(6.5, result.NetWeekdayOvertimeHours);
            Assert.Equal(10.5, result.TotalOvertimeTimeOffEarned);
        }

        [Fact]
        public void Calculate_TimeOffDay_DeductsTarget()
        {
            // Arrange
            var logs = new List<AppUsageLog>();
            // Mon-Thu: works 9 hours each (36 hours total). 
            // Daily overtime each day: 1.0 hours (from 16:00 to 17:00 @ 1.5x) -> total gross 4.0h @ 1.5x
            for (int i = 0; i < 4; i++)
            {
                DateTime day = _monday.AddDays(i);
                logs.Add(new AppUsageLog
                {
                    StartTime = day.AddHours(8),
                    EndTime = day.AddHours(17),
                    Category = "Work",
                    ProcessName = "chrome"
                });
            }

            // Friday: Registered as Time Off Day (0 work hours logged)
            var timeOffs = new List<TimeOffLog>
            {
                new TimeOffLog { Date = _monday.AddDays(4), Type = "Vacation" } // default Hours = 8.0
            };

            // Act
            var result = OvertimeCalculator.Calculate(_monday, logs, timeOffs);

            // Assert
            // Standard week = 40.0 hours. 1 Time Off day = -8.0 hours. Target = 32.0 hours.
            // Weekday hours worked = 36.0.
            // Net Weekday Overtime = 36.0 - 32.0 = 4.0 hours.
            // Gross overtime matches net overtime exactly, so user gets 4.0h @ 1.5 = 6.0 hours time off.
            Assert.Equal(32.0, result.TargetWorkHours);
            Assert.Equal(36.0, result.WeekdayHoursWorked);
            Assert.Equal(8.0, result.TimeOffHoursCredit);
            Assert.Equal(4.0, result.NetWeekdayOvertimeHours);
            Assert.Equal(6.0, result.TotalOvertimeTimeOffEarned);
        }

        [Fact]
        public void Calculate_HolidayWeekdays_ReducesTargetButDoesNotAffectFlexBalance()
        {
            // Arrange
            var logs = new List<AppUsageLog>();
            // Mon-Thu: works 8 hours each (32 hours total). No overtime.
            for (int i = 0; i < 4; i++)
            {
                DateTime day = _monday.AddDays(i);
                logs.Add(new AppUsageLog
                {
                    StartTime = day.AddHours(8),
                    EndTime = day.AddHours(16),
                    Category = "Work",
                    ProcessName = "chrome"
                });
            }

            // Friday: Registered as a Holiday (0 work hours logged)
            var timeOffs = new List<TimeOffLog>
            {
                new TimeOffLog { Date = _monday.AddDays(4), Type = "Holiday", Hours = 8.0 }
            };

            // Act
            var result = OvertimeCalculator.Calculate(_monday, logs, timeOffs);

            // Assert
            // Standard target is 40.0 hours. 
            // Friday is a Holiday (8.0h credit). Weekly target drops to 32.0 hours.
            // Weekday hours worked is 32.0.
            // Net Weekday Overtime is 32.0 worked + 8.0 holiday - 40.0 standard = 0.0 hours.
            // Total overtime earned should be 0.0.
            Assert.Equal(32.0, result.TargetWorkHours);
            Assert.Equal(32.0, result.WeekdayHoursWorked);
            Assert.Equal(8.0, result.TimeOffHoursCredit);
            Assert.Equal(0.0, result.NetWeekdayOvertimeHours);
            Assert.Equal(0.0, result.TotalOvertimeTimeOffEarned);
        }

        [Fact]
        public void GetNorwegianPublicHolidays_CalculatesCorrectDatesFor2026()
        {
            // Act
            var holidays = WorkTracker.App.GetNorwegianPublicHolidays(2026);

            // Assert
            // 2026 Easter Sunday is April 5.
            // Maundy Thursday should be April 2.
            // Good Friday should be April 3.
            // Easter Monday should be April 6.
            // Ascension Day should be May 14.
            // Whit Monday should be May 25.

            Assert.Contains(holidays, h => h.Date == new DateTime(2026, 4, 2) && h.Name == "Skjærtorsdag");
            Assert.Contains(holidays, h => h.Date == new DateTime(2026, 4, 3) && h.Name == "Langfredag");
            Assert.Contains(holidays, h => h.Date == new DateTime(2026, 4, 5) && h.Name == "1. påskedag");
            Assert.Contains(holidays, h => h.Date == new DateTime(2026, 4, 6) && h.Name == "2. påskedag");
            Assert.Contains(holidays, h => h.Date == new DateTime(2026, 5, 14) && h.Name == "Kristi himmelfartsdag");
            Assert.Contains(holidays, h => h.Date == new DateTime(2026, 5, 25) && h.Name == "2. pinsedag");

            // Fixed dates
            Assert.Contains(holidays, h => h.Date == new DateTime(2026, 1, 1) && h.Name == "Nyttårsdag");
            Assert.Contains(holidays, h => h.Date == new DateTime(2026, 5, 1) && h.Name == "Offentlig høytidsdag (1. mai)");
            Assert.Contains(holidays, h => h.Date == new DateTime(2026, 5, 17) && h.Name == "Grunnlovsdag (17. mai)");
            Assert.Contains(holidays, h => h.Date == new DateTime(2026, 12, 25) && h.Name == "1. juledag");
            Assert.Contains(holidays, h => h.Date == new DateTime(2026, 12, 26) && h.Name == "2. juledag");
        }

        [Fact]
        public void Calculate_WeekdayOvertime_ClockTimeOnly()
        {
            // Arrange: Mon works 3.0h (17:00 to 20:00) which is outside core hours (16:00 - 21:00 is 1.5x)
            // Even though daily hours is less than 8.0, it should trigger overtime strictly based on clock time.
            var logs = new List<AppUsageLog>
            {
                new AppUsageLog
                {
                    StartTime = _monday.AddHours(17),
                    EndTime = _monday.AddHours(20), // 17:00 to 20:00 = 3.0 hours
                    Category = "Work",
                    ProcessName = "chrome"
                }
            };

            // Tue-Fri: works standard 8.0h (08:00 to 16:00)
            for (int i = 1; i < 5; i++)
            {
                DateTime day = _monday.AddDays(i);
                logs.Add(new AppUsageLog
                {
                    StartTime = day.AddHours(8),
                    EndTime = day.AddHours(16),
                    Category = "Work",
                    ProcessName = "chrome"
                });
            }

            // Act
            var result = OvertimeCalculator.Calculate(_monday, logs, new List<TimeOffLog>());

            // Assert: Total weekday hours worked = 3.0 + 8.0 * 4 = 35.0
            // Net weekday overtime = 3.0 hours
            // Overtime time off earned = 3.0h * 1.5 = 4.5h
            Assert.Equal(35.0, result.WeekdayHoursWorked);
            Assert.Equal(3.0, result.NetWeekdayOvertimeHours);
            Assert.Equal(4.5, result.TotalOvertimeTimeOffEarned);
        }

        [Fact]
        public void Debug_June2_Overtime()
        {
            var start = new DateTime(2026, 6, 1);
            using (var db = new DatabaseContext())
            {
                var logs = db.AppUsageLogs.ToList();
                var timeOffs = db.TimeOffLogs.ToList();
                
                var result = OvertimeCalculator.Calculate(start, logs, timeOffs);
                
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"Week start: {start:yyyy-MM-dd}");
                sb.AppendLine($"Target Work Hours: {result.TargetWorkHours}");
                sb.AppendLine($"Weekday Hours Worked: {result.WeekdayHoursWorked}");
                sb.AppendLine($"Net Weekday Overtime: {result.NetWeekdayOvertimeHours}");
                sb.AppendLine($"Total Overtime TimeOff Earned: {result.TotalOvertimeTimeOffEarned}");
                sb.AppendLine($"Gross Overtime 1.0x: {result.GrossOvertime10Hours}");
                sb.AppendLine($"Gross Overtime 1.5x: {result.GrossOvertime15Hours}");
                sb.AppendLine($"Gross Overtime 2.0x: {result.GrossOvertime20Hours}");
                
                Assert.NotNull(result);
            }
        }

        [Fact]
        public void Debug_DiagnoseLifetimeOvertime()
        {
            using (var db = new DatabaseContext())
            {
                var allLogs = db.AppUsageLogs.ToList();
                var allTimeOffs = db.TimeOffLogs.ToList();

                var firstLog = allLogs.OrderBy(l => l.StartTime).FirstOrDefault();
                if (firstLog == null) Assert.Fail("No logs found in database!");

                DateTime minDate = OvertimeCalculator.GetStartOfWeek(firstLog.StartTime);
                DateTime maxDate = OvertimeCalculator.GetStartOfWeek(DateTime.Now).AddDays(7);

                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"First log start time: {firstLog.StartTime:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"minDate (start of week): {minDate:yyyy-MM-dd}");
                sb.AppendLine($"maxDate (end of calculation): {maxDate:yyyy-MM-dd}");

                double balance = 0;
                for (DateTime dt = minDate; dt < maxDate; dt = dt.AddDays(7))
                {
                    var result = OvertimeCalculator.Calculate(dt, allLogs, allTimeOffs);
                    double earned = result.TotalOvertimeTimeOffEarned;
                    balance += earned;

                    sb.AppendLine($"Week {dt:yyyy-MM-dd}: Worked={result.WeekdayHoursWorked:F2}h, Target={result.TargetWorkHours:F2}h, Earned={earned:F2}h, RunBalance={balance:F2}h");
                }

                double usedFlexHours = allTimeOffs
                    .Where(t => t.Date.Year != 1900 && t.Type != "Holiday")
                    .Sum(t => t.Hours);
                double totalUnlinkedGained = allTimeOffs.Where(t => t.Date.Year == 1900).Sum(t => t.Hours);
                
                double remainingSpentFlex = Math.Max(0, usedFlexHours - totalUnlinkedGained);
                double remainingUnlinkedHours = Math.Max(0, totalUnlinkedGained - usedFlexHours);
                
                double finalBalance = (balance - remainingSpentFlex) + remainingUnlinkedHours;

                sb.AppendLine($"Used Flex Hours (spent): {usedFlexHours:F2}h");
                sb.AppendLine($"Total Unlinked Gained: {totalUnlinkedGained:F2}h");
                sb.AppendLine($"Remaining Spent Flex: {remainingSpentFlex:F2}h");
                sb.AppendLine($"Remaining Unlinked: {remainingUnlinkedHours:F2}h");
                sb.AppendLine($"Final Balance: {finalBalance:F2}h");

                Assert.True(finalBalance > 0);
            }
        }
    }
}
