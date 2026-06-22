using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using WorkTracker.ViewModels;

namespace WorkTracker.Views
{

    public partial class MainWindow : Window
    {
        public System.Windows.Input.ICommand EditManualWorkCommand { get; }
        public System.Windows.Input.ICommand EditUnlinkedCommand   { get; }

        public MainWindow()
        {
            InitializeComponent();
            
            var mainVm = new MainViewModel();
            DataContext = mainVm;

            EditManualWorkCommand = new RelayCommand<DailyEarningsItem>(EditManualWork);
            EditUnlinkedCommand   = new RelayCommand(EditUnlinkedHours);
        }

        private void BtnSettings_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                var settingsWin = new SettingsWindow(vm) { Owner = this };
                settingsWin.ShowDialog();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            if (WindowState == WindowState.Minimized)
            {
                Hide(); // Minimize to system tray (hide from taskbar)
                if (System.Windows.Application.Current is App app)
                {
                    app.NotifyMinimizedToTray();
                }
            }
        }

        private void EditManualWork(DailyEarningsItem? item)
        {
            if (item == null) return;

            DateTime date = item.Date;

            double normal  = 0;
            double ot16    = 0;
            double ot21    = 0;
            double timeOff = 0;
            double sickDay = 0;

            try
            {
                using var db = new WorkTracker.Data.DatabaseContext();
                var start = date.Date;
                var end   = start.AddDays(1);

                // Read existing manual offline work logs
                var manualLogs = db.AppUsageLogs
                    .Where(l => l.StartTime >= start && l.StartTime < end
                             && l.ProcessName == "offline" && l.WindowTitle.StartsWith("Manual"))
                    .ToList();

                foreach (var log in manualLogs)
                {
                    double hrs = (log.EndTime - log.StartTime).TotalHours;
                    if      (log.WindowTitle.Contains("Normal"))                          normal += hrs;
                    else if (log.WindowTitle.Contains("16-21"))                           ot16   += hrs;
                    else if (log.WindowTitle.Contains("21-08") || log.WindowTitle.Contains("Weekend")) ot21 += hrs;
                }

                // Read existing vacation time-off log
                var existingTimeOff = db.TimeOffLogs.Find(start);
                if (existingTimeOff != null)
                    timeOff = existingTimeOff.Hours;

                // Read existing sick day logs
                var existingSick = db.AppUsageLogs
                    .Where(l => l.StartTime >= start && l.StartTime < end && l.ProcessName == "sick_time")
                    .ToList();
                sickDay = existingSick.Sum(l => (l.EndTime - l.StartTime).TotalHours);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching manual hours: {ex.Message}");
            }

            var dialog = new ManualWorkWindow(date, normal, ot16, ot21, timeOff, sickDay)
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using var db = new WorkTracker.Data.DatabaseContext();
                    var start = date.Date;
                    var end   = start.AddDays(1);

                    // ── Offline manual work ──────────────────────────────────────────────
                    var oldManual = db.AppUsageLogs
                        .Where(l => l.StartTime >= start && l.StartTime < end
                                 && l.ProcessName == "offline" && l.WindowTitle.StartsWith("Manual"))
                        .ToList();
                    db.AppUsageLogs.RemoveRange(oldManual);

                    if (dialog.NormalHours > 0)
                        db.AppUsageLogs.Add(new WorkTracker.Data.AppUsageLog
                        {
                            ProcessName = "offline",
                            WindowTitle = "Manual Offline Work (Normal)",
                            StartTime   = start.AddHours(9),
                            EndTime     = start.AddHours(9).AddHours(dialog.NormalHours),
                            Category    = "Offline Work"
                        });

                    if (dialog.Ot16Hours > 0)
                        db.AppUsageLogs.Add(new WorkTracker.Data.AppUsageLog
                        {
                            ProcessName = "offline",
                            WindowTitle = "Manual Offline Work (Overtime 16-21)",
                            StartTime   = start.AddHours(17),
                            EndTime     = start.AddHours(17).AddHours(dialog.Ot16Hours),
                            Category    = "Offline Work"
                        });

                    if (dialog.Ot21Hours > 0)
                        db.AppUsageLogs.Add(new WorkTracker.Data.AppUsageLog
                        {
                            ProcessName = "offline",
                            WindowTitle = "Manual Offline Work (Overtime 21-08/Weekend)",
                            StartTime   = start.AddHours(22),
                            EndTime     = start.AddHours(22).AddHours(dialog.Ot21Hours),
                            Category    = "Offline Work"
                        });

                    // ── Vacation / Time Off (deducts from flex balance) ──────────────────
                    var existingTimeOff = db.TimeOffLogs.Find(start);
                    if (dialog.TimeOffHours > 0)
                    {
                        if (existingTimeOff != null)
                            existingTimeOff.Hours = dialog.TimeOffHours;
                        else
                            db.TimeOffLogs.Add(new WorkTracker.Data.TimeOffLog
                            {
                                Date  = start,
                                Type  = "Vacation",
                                Hours = dialog.TimeOffHours
                            });
                    }
                    else
                    {
                        if (existingTimeOff != null)
                            db.TimeOffLogs.Remove(existingTimeOff);
                    }

                    // ── Sick Day (reduces work target, does NOT deduct flex) ──────────────
                    var oldSick = db.AppUsageLogs
                        .Where(l => l.StartTime >= start && l.StartTime < end && l.ProcessName == "sick_time")
                        .ToList();
                    db.AppUsageLogs.RemoveRange(oldSick);

                    if (dialog.SickDayHours > 0)
                        db.AppUsageLogs.Add(new WorkTracker.Data.AppUsageLog
                        {
                            ProcessName = "sick_time",
                            WindowTitle = "Sick Day",
                            StartTime   = start.AddHours(8),
                            EndTime     = start.AddHours(8).AddHours(dialog.SickDayHours),
                            Category    = "Sick Day"
                        });

                    db.SaveChanges();

                    if (DataContext is MainViewModel vm)
                        vm.RefreshData();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        $"Failed to save manual work/time off: {ex.Message}",
                        "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void EditUnlinkedHours()
        {
            if (DataContext is MainViewModel vm)
            {
                double currentHours = vm.UnlinkedTimeOffTotal;
                var dialog = new UnlinkedHoursWindow(currentHours)
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        using var db = new WorkTracker.Data.DatabaseContext();
                        var oldUnlinked = db.TimeOffLogs.Where(t => t.Date.Year == 1900).ToList();
                        db.TimeOffLogs.RemoveRange(oldUnlinked);

                        if (dialog.Hours > 0)
                            db.TimeOffLogs.Add(new WorkTracker.Data.TimeOffLog
                            {
                                Date  = new DateTime(1900, 1, 1),
                                Type  = "Unlinked",
                                Hours = dialog.Hours
                            });

                        db.SaveChanges();
                        vm.RefreshData();
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show(
                            $"Failed to save unlinked hours: {ex.Message}",
                            "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void MinimizeBtn_Click(object sender, RoutedEventArgs e) =>
            SystemCommands.MinimizeWindow(this);

        private void MaximizeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                SystemCommands.RestoreWindow(this);
            else
                SystemCommands.MaximizeWindow(this);
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();

        private void TimelineBlockEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is TimelineBlock block)
            {
                if (DataContext is MainViewModel vm)
                {
                    var dialog = new EditTimelineBlockWindow(block, vm.SelectedDate)
                    {
                        Owner = this,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        vm.SelectedTimelineBlock = block;
                        vm.SelectedBlockCategory = dialog.SelectedBlockCategory;
                        vm.SelectedBlockDescription = dialog.SelectedBlockDescription;

                        if (vm.SaveTimelineBlockCommand.CanExecute(null))
                        {
                            vm.SaveTimelineBlockCommand.Execute(null);
                        }
                    }
                }
            }
        }
    }
}
