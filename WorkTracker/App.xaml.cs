using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using WorkTracker.Data;
using WorkTracker.Services;
using WorkTracker.Views;
using WorkTracker.ViewModels;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Application = System.Windows.Application;

namespace WorkTracker
{
    public partial class App : Application
    {
        private NotifyIcon? _notifyIcon;
        private ActivityTracker? _tracker;
        private Dictionary<string, string> _categoryCache = new();
        private bool _isExitTriggered = false;
        private bool _inClosingHandler = false;
        private Views.MainWindow? _mainWindow;

        // Hourly calendar sync
        private System.Timers.Timer? _calendarSyncTimer;
        private volatile bool _isSyncing = false;

        // Session lock tracking
        private DateTime? _lockTime;
        private bool _wasLocked = false;

        public ActivityTracker Tracker => _tracker!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1. Initialize SQLite Database
            try
            {
                using var db = new DatabaseContext();
                db.Database.EnsureCreated();
                EnsureMigrated(db); // safely add any new tables to existing DBs
                ImportNorwegianHolidaysOnBoot(db); // import Norwegian public holidays

                // Cache categories in memory for performance
                _categoryCache = db.AppCategories.ToDictionary(
                    c => c.ProcessName,
                    c => c.CategoryName,
                    StringComparer.OrdinalIgnoreCase
                );
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to initialize database: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            // 2. Initialize Activity Tracker
            // Idle threshold: 5 minutes (300 seconds). For quick tests, you can set it lower, but 300s is standard.
            _tracker = new ActivityTracker(ResolveCategory, idleThresholdSeconds: 300);
            _tracker.ActivityLogged += OnActivityLogged;
            _tracker.UserReturnedFromIdle += OnUserReturnedFromIdle;
            _tracker.Start();

            // 3. Setup System Tray Icon
            SetupTrayIcon();

            // 4. Show main window
            _mainWindow = new Views.MainWindow();
            _mainWindow.Closing += MainWindow_Closing;
            _mainWindow.Show();

            // 5. Start calendar sync (runs every hour, also immediately on startup)
            InitCalendarSync();

            // 6. Monitor session lock/unlock events to capture AFK sessions
            Microsoft.Win32.SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;

            // 7. Auto check for updates on startup
            CheckForUpdatesOnStartup();
        }

        private void SetupTrayIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = CreateAppIcon(),
                Text = "Work Tracker",
                Visible = true
            };

            _notifyIcon.DoubleClick += (s, e) => ShowMainWindow();
            _notifyIcon.MouseClick += NotifyIcon_MouseClick;

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Open Work Tracker", null, (s, e) => ShowMainWindow());
            
            // Start app on Boot checkable menu item
            var bootMenuItem = new ToolStripMenuItem("Start app on Boot")
            {
                CheckOnClick = true,
                Checked = IsStartOnBootEnabled()
            };
            bootMenuItem.Click += (s, e) => SetStartOnBoot(bootMenuItem.Checked);
            contextMenu.Items.Add(bootMenuItem);

            contextMenu.Items.Add("Restart", null, (s, e) => RestartApplication());
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Exit", null, (s, e) => ExitApplication());

            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private TrayPopupWindow? _popupWindow;
        private DateTime _lastDeactivatedTime = DateTime.MinValue;

        public void RegisterDeactivation()
        {
            _lastDeactivatedTime = DateTime.Now;
        }

        private void NotifyIcon_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // If popup deactivated within the last 250ms, ignore click (it was closed by deactivation from this click)
                if ((DateTime.Now - _lastDeactivatedTime).TotalMilliseconds < 250)
                {
                    return;
                }

                if (_popupWindow != null)
                {
                    try
                    {
                        _popupWindow.Close();
                    }
                    catch { }
                    _popupWindow = null;
                }
                else
                {
                    MainViewModel? vm = _mainWindow?.DataContext as MainViewModel;
                    if (vm != null)
                    {
                        vm.RefreshData();
                        _popupWindow = new TrayPopupWindow(vm);
                        _popupWindow.Closed += (s, ev) => _popupWindow = null;
                        _popupWindow.Show();
                        _popupWindow.Activate();
                    }
                }
            }
        }

        private const string StartupRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string StartupAppName = "WorkTracker";

        private bool IsStartOnBootEnabled()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(StartupRegistryKey);
                if (key != null)
                {
                    return key.GetValue(StartupAppName) != null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking registry: {ex.Message}");
            }
            return false;
        }

        private void SetStartOnBoot(bool enable)
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(StartupRegistryKey, writable: true);
                if (key != null)
                {
                    if (enable)
                    {
                        string path = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                        if (!string.IsNullOrEmpty(path))
                        {
                            key.SetValue(StartupAppName, $"\"{path}\"");
                        }
                    }
                    else
                    {
                        key.DeleteValue(StartupAppName, throwOnMissingValue: false);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting registry startup: {ex.Message}");
            }
        }

        private void RestartApplication()
        {
            try
            {
                string path = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                if (!string.IsNullOrEmpty(path))
                {
                    Process.Start(path);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to restart: {ex.Message}", "Restart Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            ExitApplication();
        }

        private Icon CreateAppIcon()
        {
            try
            {
                // Attempt to load the compiled app_icon.ico resource directly to preserve transparency and multi-size quality
                var uri = new Uri("pack://application:,,,/app_icon.ico");
                var streamInfo = System.Windows.Application.GetResourceStream(uri);
                if (streamInfo != null)
                {
                    using var stream = streamInfo.Stream;
                    return new Icon(stream);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load app_icon.ico for tray: {ex.Message}");
            }

            // Fallback to dynamic drawing if resource is missing
            try
            {
                using Bitmap bmp = new Bitmap(32, 32);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.Clear(Color.Transparent);

                    using Brush bgBrush = new SolidBrush(Color.FromArgb(99, 102, 241)); // Indigo #6366F1
                    g.FillEllipse(bgBrush, 2, 2, 28, 28);

                    using Pen ringPen = new Pen(Color.White, 2f);
                    g.DrawEllipse(ringPen, 3, 3, 26, 26);

                    using Font font = new Font("Segoe UI", 13, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel);
                    using Brush textBrush = new SolidBrush(Color.White);
                    g.DrawString("W", font, textBrush, 9, 7);
                }
                return Icon.FromHandle(bmp.GetHicon());
            }
            catch
            {
                return SystemIcons.Application;
            }
        }

        public void ShowMainWindow()
        {
            if (_mainWindow != null)
            {
                if (_mainWindow.WindowState == WindowState.Minimized)
                {
                    _mainWindow.WindowState = WindowState.Normal;
                }
                _mainWindow.Show();
                _mainWindow.Activate();
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_isExitTriggered && _mainWindow != null)
            {
                e.Cancel = true; // Block actual closing by default
                
                var prompt = new ClosePromptWindow
                {
                    Owner = _mainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                if (prompt.ShowDialog() == true)
                {
                    if (prompt.ResultAction == CloseAction.MinimizeToTray)
                    {
                        _mainWindow.Hide();
                        NotifyMinimizedToTray();
                    }
                    else if (prompt.ResultAction == CloseAction.ExitApplication)
                    {
                        e.Cancel = false; // Allow the close to proceed
                        _inClosingHandler = true;
                        try
                        {
                            ExitApplication();
                        }
                        finally
                        {
                            _inClosingHandler = false;
                        }
                    }
                }
            }
        }

        public void NotifyMinimizedToTray()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.ShowBalloonTip(
                    3000,
                    "Work Tracker",
                    "Application minimized to system tray. Double-click the icon to restore.",
                    ToolTipIcon.Info
                );
            }
        }

        public void ExitApplication()
        {
            _isExitTriggered = true;
            _tracker?.Stop();
            _tracker?.Dispose();

            // Stop calendar sync timer
            _calendarSyncTimer?.Stop();
            _calendarSyncTimer?.Dispose();
            _calendarSyncTimer = null;

            // Unsubscribe from session switch events
            Microsoft.Win32.SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;

            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }

            if (!_inClosingHandler && _mainWindow != null)
            {
                _mainWindow.Close();
            }
            Shutdown();
        }

        #region ICS Calendar Auto-Sync

        /// <summary>
        /// Safely adds the AppSettings table to existing databases that were created
        /// before this column existed. EnsureCreated() only creates tables for a brand-new DB.
        /// </summary>
        private static void EnsureMigrated(DatabaseContext db)
        {
            try
            {
                db.Database.ExecuteSqlRaw(
                    @"CREATE TABLE IF NOT EXISTS AppSettings (
                        Key   TEXT NOT NULL PRIMARY KEY,
                        Value TEXT NOT NULL DEFAULT ''
                    );");

                db.Database.ExecuteSqlRaw(
                    @"CREATE TABLE IF NOT EXISTS HolidayLogs (
                        Id        INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        StartDate TEXT NOT NULL,
                        EndDate   TEXT NOT NULL,
                        Note      TEXT NOT NULL DEFAULT ''
                    );");

                // Safely alter HolidayLogs to add IsPublicHoliday if it doesn't exist
                try
                {
                    db.Database.ExecuteSqlRaw("ALTER TABLE HolidayLogs ADD COLUMN IsPublicHoliday INTEGER NOT NULL DEFAULT 0;");
                }
                catch { }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Migration] database tables: {ex.Message}");
            }
        }

        public static DateTime GetEasterSunday(int year)
        {
            int a = year % 19;
            int b = year / 100;
            int c = year % 100;
            int d = b / 4;
            int e = b % 4;
            int f = (b + 8) / 25;
            int g = (b - f + 1) / 3;
            int h = (19 * a + b - d - g + 15) % 30;
            int i = c / 4;
            int k = c % 4;
            int l = (32 + 2 * e + 2 * i - h - k) % 7;
            int m = (a + 11 * h + 22 * l) / 451;
            int month = (h + l - 7 * m + 114) / 31;
            int day = ((h + l - 7 * m + 114) % 31) + 1;
            return new DateTime(year, month, day);
        }

        public static List<(DateTime Date, string Name)> GetNorwegianPublicHolidays(int year)
        {
            var list = new List<(DateTime Date, string Name)>();

            // Fixed dates
            list.Add((new DateTime(year, 1, 1), "Nyttårsdag"));
            list.Add((new DateTime(year, 5, 1), "Offentlig høytidsdag (1. mai)"));
            list.Add((new DateTime(year, 5, 17), "Grunnlovsdag (17. mai)"));
            list.Add((new DateTime(year, 12, 25), "1. juledag"));
            list.Add((new DateTime(year, 12, 26), "2. juledag"));

            // Moveable dates (based on Easter)
            DateTime easterSunday = GetEasterSunday(year);
            list.Add((easterSunday.AddDays(-3), "Skjærtorsdag"));
            list.Add((easterSunday.AddDays(-2), "Langfredag"));
            list.Add((easterSunday, "1. påskedag"));
            list.Add((easterSunday.AddDays(1), "2. påskedag"));
            list.Add((easterSunday.AddDays(39), "Kristi himmelfartsdag"));
            list.Add((easterSunday.AddDays(49), "1. pinsedag"));
            list.Add((easterSunday.AddDays(50), "2. pinsedag"));

            return list;
        }

        private void ImportNorwegianHolidaysOnBoot(DatabaseContext db)
        {
            try
            {
                int currentYear = DateTime.Today.Year;
                int nextYear = currentYear + 1;

                var holidays = new List<(DateTime Date, string Name)>();
                holidays.AddRange(GetNorwegianPublicHolidays(currentYear));
                holidays.AddRange(GetNorwegianPublicHolidays(nextYear));

                bool addedAny = false;
                foreach (var h in holidays)
                {
                    // Check if there is already a HolidayLog for this specific date and isPublicHoliday
                    bool exists = db.HolidayLogs.Any(x => x.StartDate == h.Date && x.IsPublicHoliday);
                    if (!exists)
                    {
                        db.HolidayLogs.Add(new HolidayLog
                        {
                            StartDate = h.Date,
                            EndDate = h.Date,
                            Note = h.Name,
                            IsPublicHoliday = true
                        });
                        addedAny = true;
                    }
                }

                if (addedAny)
                {
                    db.SaveChanges();
                    Debug.WriteLine("[Holidays] Imported new Norwegian public holidays.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Holidays] Failed to import public holidays: {ex.Message}");
            }
        }

        private static double GetMillisecondsToNextHour()
        {
            DateTime now = DateTime.Now;
            DateTime nextHour = now.AddHours(1);
            nextHour = new DateTime(nextHour.Year, nextHour.Month, nextHour.Day, nextHour.Hour, 0, 0, 0, now.Kind);
            return (nextHour - now).TotalMilliseconds;
        }

        private void InitCalendarSync()
        {
            // Run an initial sync immediately in the background
            RunCalendarSync();

            // Then set a timer to trigger on the next hour on the hour
            _calendarSyncTimer = new System.Timers.Timer(GetMillisecondsToNextHour())
            {
                AutoReset = false
            };
            _calendarSyncTimer.Elapsed += CalendarSyncTimer_Elapsed;
            _calendarSyncTimer.Start();
        }

        private void CalendarSyncTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            RunCalendarSync();
            
            // Re-schedule for the next hour on the hour
            if (_calendarSyncTimer != null)
            {
                _calendarSyncTimer.Interval = GetMillisecondsToNextHour();
                _calendarSyncTimer.Start();
            }
        }

        /// <summary>Called by MainViewModel when the user saves a new ICS URL.</summary>
        public void RefreshIcsUrl() => RunCalendarSync();

        private void RunCalendarSync()
        {
            if (_isSyncing) return;
            var thread = new System.Threading.Thread(SyncCalendar)
            {
                IsBackground = true,
                Name = "CalendarSyncThread"
            };
            thread.Start();
        }

        private static bool IsTimeOffSubject(string subject)
        {
            if (string.IsNullOrWhiteSpace(subject)) return false;
            string lower = subject.ToLowerInvariant();
            if (lower.Contains("avspas")) return true;
            
            var tokens = lower.Split(new[] { ' ', '.', ',', '-', '_', '/', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                if (token == "fri" || token == "fridag")
                    return true;
            }
            return false;
        }

        private static bool IsExcludedSubject(string subject)
        {
            if (string.IsNullOrWhiteSpace(subject)) return false;
            string lower = subject.ToLowerInvariant();
            
            // Exclude keywords: Dinner, Middag, Party, Fest, Lunch, Lunsj, Private, Privat
            string[] excludedKeywords = { "dinner", "middag", "party", "fest", "lunch", "lunsj", "private", "privat" };
            foreach (var kw in excludedKeywords)
            {
                if (lower.Contains(kw))
                    return true;
            }
            return false;
        }

        private void CleanupExcludedEvents(DatabaseContext db)
        {
            try
            {
                var badLogs = db.AppUsageLogs
                    .Where(l => l.ProcessName == "ics_import" || l.ProcessName == "outlook_import")
                    .ToList()
                    .Where(l => IsExcludedSubject(l.WindowTitle))
                    .ToList();

                if (badLogs.Count > 0)
                {
                    db.AppUsageLogs.RemoveRange(badLogs);
                    db.SaveChanges();
                    Debug.WriteLine($"[Cleanup] Removed {badLogs.Count} previously imported excluded events.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Cleanup] Failed: {ex.Message}");
            }
        }

        private void CleanupFutureWorkEvents(DatabaseContext db)
        {
            try
            {
                var now = DateTime.Now;
                var futureWork = db.AppUsageLogs
                    .Where(l => l.ProcessName == "ics_import" || l.ProcessName == "outlook_import")
                    .ToList()
                    .Where(l => l.StartTime > now && !IsTimeOffSubject(l.WindowTitle))
                    .ToList();

                if (futureWork.Count > 0)
                {
                    db.AppUsageLogs.RemoveRange(futureWork);
                    db.SaveChanges();
                    Debug.WriteLine($"[Cleanup] Removed {futureWork.Count} future work events.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Cleanup] Future events cleanup failed: {ex.Message}");
            }
        }

        private void SyncCalendar()
        {
            if (_isSyncing) return;
            _isSyncing = true;
            
            Dispatcher.BeginInvoke(() =>
            {
                if (_mainWindow?.DataContext is MainViewModel mainVm)
                    mainVm.IcsSyncStatus = "🔄 Syncing calendar...";
            });

            AppLogger.Log("Calendar sync started.");

            try
            {
                // Clean up database from previously imported excluded events
                using (var db = new DatabaseContext())
                {
                    CleanupExcludedEvents(db);
                    ImportNorwegianHolidaysOnBoot(db);
                }

                // Read settings fresh from the database each time
                string icsUrl = string.Empty;
                DateTime? minImportDate = null;
                using (var db = new DatabaseContext())
                {
                    icsUrl = db.AppSettings.Find("IcsCalendarUrl")?.Value ?? string.Empty;
                    
                    string minDateStr = db.AppSettings.Find("IcsMinImportDate")?.Value ?? string.Empty;
                    if (DateTime.TryParse(minDateStr, out DateTime parsedDate))
                    {
                        minImportDate = parsedDate.Date;
                    }
                }

                AppLogger.Log($"Syncing ICS URL: {(string.IsNullOrEmpty(icsUrl) ? "[None]" : "Configured")}");

                // Sync: from minImportDate (if configured and earlier than current week start) to 1 year in the future
                DateTime weekStart = OvertimeCalculator.GetStartOfWeek(DateTime.Today);
                DateTime rangeStart = minImportDate.HasValue && minImportDate.Value < weekStart
                    ? minImportDate.Value
                    : weekStart;
                DateTime rangeEnd   = DateTime.Today.AddYears(1);

                // 1. Sync ICS calendar (if configured)
                List<IcsEventItem> icsEvents = new();
                if (!string.IsNullOrWhiteSpace(icsUrl))
                {
                    try
                    {
                        AppLogger.Log($"Fetching ICS events from {rangeStart:yyyy-MM-dd} to {rangeEnd:yyyy-MM-dd}...");
                        icsEvents = IcsCalendarService.GetCalendarEventsAsync(icsUrl, rangeStart, rangeEnd)
                                                       .GetAwaiter().GetResult();
                        AppLogger.Log($"Fetched {icsEvents?.Count ?? 0} calendar events from ICS.");
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Log($"[IcsSync] Failed to fetch/parse ICS: {ex.Message}");
                    }
                }

                int added = 0;
                using (var db = new DatabaseContext())
                {
                    var uniqueWorkLogs = new List<AppUsageLog>();
                    var seenWorkKeys = new HashSet<string>();

                    // Process ICS events
                    if (icsEvents != null)
                    {
                        foreach (var evt in icsEvents)
                        {
                            if (IsExcludedSubject(evt.Subject)) continue;
                            bool isTimeOff = IsTimeOffSubject(evt.Subject);
                            if (minImportDate.HasValue && evt.Start.Date < minImportDate.Value) continue;

                            // Skip regular all-day/multi-day events!
                            if (evt.IsAllDay || (evt.End - evt.Start).TotalHours >= 24 || evt.Start.Date != evt.End.Date)
                            {
                                if (!isTimeOff) continue;
                            }

                            if (isTimeOff)
                            {
                                DateTime startDay = evt.Start.Date;
                                DateTime endDay = evt.End.Date;
                                if (evt.End.TimeOfDay > TimeSpan.Zero || endDay == startDay)
                                {
                                    endDay = endDay.AddDays(1);
                                }

                                for (DateTime day = startDay; day < endDay; day = day.AddDays(1))
                                {
                                    double duration = 8.0;
                                    if (!evt.IsAllDay)
                                    {
                                        DateTime dayStart = day;
                                        DateTime dayEnd = day.AddDays(1);
                                        DateTime overlapStart = evt.Start > dayStart ? evt.Start : dayStart;
                                        DateTime overlapEnd = evt.End < dayEnd ? evt.End : dayEnd;
                                        duration = (overlapEnd - overlapStart).TotalHours;
                                        if (duration >= 8.0) duration = 8.0;
                                    }

                                    var existingTimeOff = db.TimeOffLogs.Find(day);
                                    if (existingTimeOff == null)
                                    {
                                        db.TimeOffLogs.Add(new TimeOffLog { Date = day, Type = "Vacation", Hours = duration });
                                        added++;
                                    }
                                    else if (Math.Abs(existingTimeOff.Hours - duration) > 0.01)
                                    {
                                        existingTimeOff.Hours = duration;
                                        added++;
                                    }
                                }
                            }
                            else
                            {
                                var key = $"ics_import|{evt.Start.Ticks}|{evt.End.Ticks}|{evt.Subject}";
                                if (!seenWorkKeys.Contains(key))
                                {
                                    seenWorkKeys.Add(key);
                                    uniqueWorkLogs.Add(new AppUsageLog
                                    {
                                        ProcessName = "ics_import",
                                        WindowTitle = evt.Subject,
                                        StartTime   = evt.Start,
                                        EndTime     = evt.End,
                                        Category    = evt.Category
                                    });
                                }
                            }
                        }
                    }

                    // Outlook events processing removed (no longer used)

                    // Synchronize AppUsageLogs in database for range [rangeStart, rangeEnd]
                    var dbExistingWorkLogs = db.AppUsageLogs
                        .Where(l => (l.ProcessName == "ics_import" || l.ProcessName == "outlook_import")
                                 && l.StartTime >= rangeStart
                                 && l.StartTime < rangeEnd)
                        .ToList();

                    var dbKeys = new HashSet<string>(dbExistingWorkLogs.Select(l => $"{l.ProcessName}|{l.StartTime.Ticks}|{l.EndTime.Ticks}|{l.WindowTitle}"));
                    var fetchedKeys = new HashSet<string>(uniqueWorkLogs.Select(l => $"{l.ProcessName}|{l.StartTime.Ticks}|{l.EndTime.Ticks}|{l.WindowTitle}"));

                    var logsToDelete = dbExistingWorkLogs
                        .Where(l => !fetchedKeys.Contains($"{l.ProcessName}|{l.StartTime.Ticks}|{l.EndTime.Ticks}|{l.WindowTitle}"))
                        .ToList();

                    var logsToInsert = uniqueWorkLogs
                        .Where(l => !dbKeys.Contains($"{l.ProcessName}|{l.StartTime.Ticks}|{l.EndTime.Ticks}|{l.WindowTitle}"))
                        .ToList();

                    if (logsToDelete.Count > 0)
                    {
                        db.AppUsageLogs.RemoveRange(logsToDelete);
                        added += logsToDelete.Count;
                    }

                    if (logsToInsert.Count > 0)
                    {
                        db.AppUsageLogs.AddRange(logsToInsert);
                        added += logsToInsert.Count;
                    }

                    if (added > 0 || logsToDelete.Count > 0) db.SaveChanges();
                }

                AppLogger.Log($"[CalendarSync] Finished. Added/Updated/Deleted events in database (Changes: {added}).");
                Dispatcher.BeginInvoke(() =>
                {
                    if (_mainWindow?.DataContext is MainViewModel mainVm)
                    {
                        string icsStatus = string.IsNullOrWhiteSpace(icsUrl) ? "No ICS URL configured." : "ICS sync complete.";
                        mainVm.IcsSyncStatus = $"✅ Sync complete! Added/Updated {added} events at {DateTime.Now:HH:mm:ss}. ({icsStatus})";
                        mainVm.RefreshData();
                    }
                });
            }
            catch (Exception ex)
            {
                AppLogger.Log($"[CalendarSync] Sync failed: {ex.Message}");
                Dispatcher.BeginInvoke(() =>
                {
                    if (_mainWindow?.DataContext is MainViewModel mainVm)
                        mainVm.IcsSyncStatus = $"❌ Sync failed: {ex.Message}";
                });
            }
            finally
            {
                _isSyncing = false;
            }
        }

        #endregion

        private string ResolveCategory(string processName)
        {
            if (string.IsNullOrEmpty(processName)) return "Work";
            processName = ActivityTracker.CleanProcessName(processName);

            lock (_categoryCache)
            {
                if (_categoryCache.TryGetValue(processName, out string? category))
                {
                    return category;
                }

                // If not in cache, add it to DB as default "Work" category and update cache
                string defaultCategory = "Work";
                try
                {
                    using var db = new DatabaseContext();
                    var existing = db.AppCategories.Find(processName);
                    if (existing == null)
                    {
                        db.AppCategories.Add(new AppCategory
                        {
                            ProcessName = processName,
                            CategoryName = defaultCategory
                        });
                        db.SaveChanges();
                    }
                    else
                    {
                        defaultCategory = existing.CategoryName;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to write new category for {processName}: {ex.Message}");
                }

                _categoryCache[processName] = defaultCategory;
                return defaultCategory;
            }
        }

        public void UpdateCategoryCache(string processName, string category)
        {
            processName = ActivityTracker.CleanProcessName(processName);
            lock (_categoryCache)
            {
                _categoryCache[processName] = category;
            }
        }

        private void OnActivityLogged(object? sender, ActivityEventArgs e)
        {
            // Run asynchronously or on worker thread to avoid blocking tracking loop
            try
            {
                using var db = new DatabaseContext();
                db.AppUsageLogs.Add(new AppUsageLog
                {
                    ProcessName = e.ProcessName,
                    WindowTitle = e.WindowTitle,
                    StartTime = e.StartTime,
                    EndTime = e.EndTime,
                    Category = e.Category
                });
                db.SaveChanges();

                // Notify MainViewModel to refresh active logs
                Dispatcher.BeginInvoke(() =>
                {
                    if (_mainWindow?.DataContext is MainViewModel mainVm)
                    {
                        mainVm.RefreshData();
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save activity log: {ex.Message}");
            }
        }

        private bool IsFocusModeEnabled()
        {
            try
            {
                using var db = new DatabaseContext();
                var setting = db.AppSettings.Find("FocusModeEnabled");
                return setting != null && setting.Value == "true";
            }
            catch
            {
                return false;
            }
        }

        private void LogAutomatedOfflineWork(DateTime startTime, double durationSeconds, string category, string description)
        {
            try
            {
                using var db = new DatabaseContext();
                db.AppUsageLogs.Add(new AppUsageLog
                {
                    ProcessName = "offline",
                    WindowTitle = description,
                    StartTime = startTime,
                    EndTime = startTime.AddSeconds(durationSeconds),
                    Category = category
                });
                db.SaveChanges();
                AppLogger.Log($"Automated idle log: {category} - {description}");

                Dispatcher.BeginInvoke(() =>
                {
                    if (_mainWindow?.DataContext is MainViewModel mainVm)
                    {
                        mainVm.RefreshData();
                    }
                });
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error saving automated idle log: {ex.Message}");
            }
        }

        private void OnUserReturnedFromIdle(object? sender, IdleEventArgs e)
        {
            // Reset lock variables
            bool isLock = _wasLocked;
            _wasLocked = false;
            _lockTime = null;

            // Suppress prompt if away period goes past 16:00, starts after 16:00, or is on a weekend
            DateTime idleEndTime = e.IdleStartTime.AddSeconds(e.IdleDurationSeconds);
            bool isWeekend = e.IdleStartTime.DayOfWeek == DayOfWeek.Saturday || e.IdleStartTime.DayOfWeek == DayOfWeek.Sunday;
            if (isWeekend || e.IdleStartTime.TimeOfDay >= new TimeSpan(16, 0, 0) || idleEndTime.TimeOfDay > new TimeSpan(16, 0, 0))
            {
                AppLogger.Log($"Suppressed idle prompt because end time {idleEndTime:HH:mm} is after 16:00, starts after 16:00, or is weekend.");
                return;
            }

            // 1. Focus Mode check
            if (IsFocusModeEnabled())
            {
                AppLogger.Log("Focus Mode is ON. Suppressing idle prompt and auto-logging offline work.");
                LogAutomatedOfflineWork(e.IdleStartTime, e.IdleDurationSeconds, "Offline Work", "Focus Mode - Auto Logged");
                return;
            }

            // 2. Full-screen active app checks
            if (e.WasFullScreen && !string.IsNullOrEmpty(e.LastProcessName))
            {
                string procLower = e.LastProcessName.ToLowerInvariant().Trim();
                if (procLower == "teams" || procLower == "ms-teams" || procLower == "zoom")
                {
                    AppLogger.Log($"Smart suppression: Full-screen meeting app ({e.LastProcessName}) detected. Auto-logging Meeting.");
                    LogAutomatedOfflineWork(e.IdleStartTime, e.IdleDurationSeconds, "Meeting", $"Meeting (Auto) - {e.LastProcessName}");
                    return;
                }
                else if (procLower == "powerpnt" || procLower == "vlc" || procLower == "chrome" || procLower == "msedge")
                {
                    AppLogger.Log($"Smart suppression: Full-screen work/media app ({e.LastProcessName}) detected. Auto-logging Offline Work.");
                    LogAutomatedOfflineWork(e.IdleStartTime, e.IdleDurationSeconds, "Offline Work", $"Work (Auto) - {e.LastProcessName}");
                    return;
                }
            }

            // Prompt user about what they did during idle period
            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    AppLogger.Log($"Prompting user for returning from idle (duration: {e.IdleDurationSeconds}s, lock: {isLock})");

                    var prompt = new IdlePromptWindow(e.IdleStartTime, e.IdleDurationSeconds, isLockPrompt: isLock)
                    {
                        Owner = _mainWindow,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };

                    if (prompt.ShowDialog() == true)
                    {
                        // Save the offline work category chosen by the user
                        if (prompt.ResultCategory == "Meeting" || prompt.ResultCategory == "Offline Work")
                        {
                            using var db = new DatabaseContext();
                            db.AppUsageLogs.Add(new AppUsageLog
                            {
                                ProcessName = "offline",
                                WindowTitle = prompt.ResultDescription,
                                StartTime = e.IdleStartTime,
                                EndTime = e.IdleStartTime.AddSeconds(e.IdleDurationSeconds),
                                Category = prompt.ResultCategory
                            });
                            db.SaveChanges();

                            AppLogger.Log($"Logged idle return choice: {prompt.ResultCategory} - {prompt.ResultDescription}");

                            if (_mainWindow?.DataContext is MainViewModel mainVm)
                            {
                                mainVm.RefreshData();
                            }
                        }
                        else
                        {
                            AppLogger.Log($"User selected: {prompt.ResultCategory} (not logged to database)");
                        }
                    }
                    else
                    {
                        AppLogger.Log("Idle prompt dialog was cancelled by user.");
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Log($"Error prompting idle return: {ex.Message}");
                }
            });
        }

        public void SimulateIdlePopup(double durationSeconds, bool isLockPrompt)
        {
            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    DateTime simulatedStartTime = DateTime.Now.AddSeconds(-durationSeconds);
                    AppLogger.Log($"Simulating idle prompt popup: duration={durationSeconds}s, lock={isLockPrompt}, startTime={simulatedStartTime:HH:mm:ss}");

                    var prompt = new IdlePromptWindow(simulatedStartTime, durationSeconds, isLockPrompt: isLockPrompt)
                    {
                        Owner = _mainWindow,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };

                    if (prompt.ShowDialog() == true)
                    {
                        if (prompt.ResultCategory == "Meeting" || prompt.ResultCategory == "Offline Work")
                        {
                            using var db = new DatabaseContext();
                            db.AppUsageLogs.Add(new AppUsageLog
                            {
                                ProcessName = "offline",
                                WindowTitle = prompt.ResultDescription,
                                StartTime = simulatedStartTime,
                                EndTime = simulatedStartTime.AddSeconds(durationSeconds),
                                Category = prompt.ResultCategory
                            });
                            db.SaveChanges();

                            AppLogger.Log($"Logged simulated return: {prompt.ResultCategory} - {prompt.ResultDescription}");

                            if (_mainWindow?.DataContext is MainViewModel mainVm)
                            {
                                mainVm.RefreshData();
                            }
                        }
                        else
                        {
                            AppLogger.Log($"Simulated prompt return choice: {prompt.ResultCategory} (not logged)");
                        }
                    }
                    else
                    {
                        AppLogger.Log("Simulated prompt dialog cancelled.");
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Log($"Error simulating idle return: {ex.Message}");
                }
            });
        }

        private void SystemEvents_SessionSwitch(object sender, Microsoft.Win32.SessionSwitchEventArgs e)
        {
            if (e.Reason == Microsoft.Win32.SessionSwitchReason.SessionLock)
            {
                _lockTime = DateTime.Now;
                _wasLocked = true;
                AppLogger.Log($"[SessionSwitch] System Locked at {_lockTime}");
            }
            else if (e.Reason == Microsoft.Win32.SessionSwitchReason.SessionUnlock)
            {
                if (_lockTime.HasValue)
                {
                    double durationSeconds = (DateTime.Now - _lockTime.Value).TotalSeconds;
                    AppLogger.Log($"[SessionSwitch] System Unlocked. Locked duration: {durationSeconds} seconds.");
                    // If the lock duration is less than the idle threshold (300 seconds),
                    // clear the lock state since the ActivityTracker won't fire UserReturnedFromIdle.
                    if (durationSeconds < 300)
                    {
                        _wasLocked = false;
                        _lockTime = null;
                    }
                }
            }
        }

        private void CheckForUpdatesOnStartup()
        {
            Task.Run(async () =>
            {
                try
                {
                    // Delay check slightly so the main window is rendered and fully loaded
                    await Task.Delay(3000);

                    AppLogger.Log("Auto update check: Initiating startup check...");
                    var updateService = new UpdateService();
                    var updateInfo = await updateService.CheckForUpdatesAsync(msg => AppLogger.Log($"[StartupUpdateCheck] {msg}"));

                    if (updateInfo.HasUpdate)
                    {
                        AppLogger.Log($"Auto update check: Newer release available: v{updateInfo.LatestVersion}");
                        
                        Dispatcher.Invoke(() =>
                        {
                            try
                            {
                                var result = System.Windows.MessageBox.Show(
                                    _mainWindow,
                                    $"A new update (v{updateInfo.LatestVersion}) is available.\n\nRelease Notes:\n{updateInfo.ReleaseNotes}\n\nWould you like to download and install it silently now?",
                                    "Update Available",
                                    MessageBoxButton.YesNo,
                                    MessageBoxImage.Question
                                );

                                if (result == MessageBoxResult.Yes)
                                {
                                    AppLogger.Log($"User accepted startup update v{updateInfo.LatestVersion}. Downloading...");
                                    
                                    Task.Run(async () =>
                                    {
                                        try
                                        {
                                            await updateService.DownloadAndInstallUpdateAsync(
                                                updateInfo.DownloadUrl,
                                                null,
                                                msg => AppLogger.Log($"[StartupUpdate] {msg}")
                                            );
                                        }
                                        catch (Exception ex)
                                        {
                                            Dispatcher.Invoke(() =>
                                            {
                                                System.Windows.MessageBox.Show(
                                                    _mainWindow,
                                                    $"Failed to install update: {ex.Message}",
                                                    "Update Error",
                                                    MessageBoxButton.OK,
                                                    MessageBoxImage.Error
                                                );
                                            });
                                        }
                                    });
                                }
                                else
                                {
                                    AppLogger.Log("User deferred startup update.");
                                }
                            }
                            catch (Exception ex)
                            {
                                AppLogger.Log($"Error prompting startup update: {ex.Message}");
                            }
                        });
                    }
                    else
                    {
                        AppLogger.Log("Auto update check: App is up to date.");
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Log($"Error in background startup update check: {ex.Message}");
                }
            });
        }
    }
}
