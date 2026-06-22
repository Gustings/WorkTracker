using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using WorkTracker.Data;
using WorkTracker.Services;

namespace WorkTracker.ViewModels
{
    public class TimelineBlock : ViewModelBase
    {
        private string _timeLabel = string.Empty;
        private string _category = "Away";
        private string _appDetails = string.Empty;
        private double _activePercentage;

        public string TimeLabel
        {
            get => _timeLabel;
            set => SetField(ref _timeLabel, value);
        }

        public string Category
        {
            get => _category;
            set => SetField(ref _category, value);
        }

        public string AppDetails
        {
            get => _appDetails;
            set => SetField(ref _appDetails, value);
        }

        public double ActivePercentage
        {
            get => _activePercentage;
            set => SetField(ref _activePercentage, value);
        }
    }

    public class TopAppViewModel : ViewModelBase
    {
        private string _processName = string.Empty;
        private string _category = "Work";
        private double _durationHours;
        private string _durationText = string.Empty;

        public string ProcessName
        {
            get => _processName;
            set => SetField(ref _processName, value);
        }

        public string Category
        {
            get => _category;
            set => SetField(ref _category, value);
        }

        public double DurationHours
        {
            get => _durationHours;
            set => SetField(ref _durationHours, value);
        }

        public string DurationText
        {
            get => _durationText;
            set => SetField(ref _durationText, value);
        }
    }

    public class DailyEarningsItem : ViewModelBase
    {
        private string _dayName = string.Empty;
        private string _dateText = string.Empty;
        private DateTime _date;
        private double _hoursWorked;
        private double _targetHours;
        private double _timeOffCredit;
        private double _gainedTimeOff;
        private double _savedUpTimeOff;

        public string DayName
        {
            get => _dayName;
            set => SetField(ref _dayName, value);
        }

        public string DateText
        {
            get => _dateText;
            set => SetField(ref _dateText, value);
        }

        public DateTime Date
        {
            get => _date;
            set => SetField(ref _date, value);
        }

        public double HoursWorked
        {
            get => _hoursWorked;
            set => SetField(ref _hoursWorked, value);
        }

        public double TargetHours
        {
            get => _targetHours;
            set => SetField(ref _targetHours, value);
        }

        public double TimeOffCredit
        {
            get => _timeOffCredit;
            set => SetField(ref _timeOffCredit, value);
        }

        public double GainedTimeOff
        {
            get => _gainedTimeOff;
            set => SetField(ref _gainedTimeOff, value);
        }

        public double SavedUpTimeOff
        {
            get => _savedUpTimeOff;
            set => SetField(ref _savedUpTimeOff, value);
        }
    }

    public class DailyChartItem : ViewModelBase
    {
        private string _dayName = string.Empty;
        private double _standardHours;
        private double _overtimeHours;
        private double _remainingTarget;
        private string _workedText = string.Empty;
        private string _targetText = string.Empty;

        public string DayName { get => _dayName; set => SetField(ref _dayName, value); }
        public double StandardHours { get => _standardHours; set => SetField(ref _standardHours, value); }
        public double OvertimeHours { get => _overtimeHours; set => SetField(ref _overtimeHours, value); }
        public double RemainingTarget { get => _remainingTarget; set => SetField(ref _remainingTarget, value); }
        public string WorkedText { get => _workedText; set => SetField(ref _workedText, value); }
        public string TargetText { get => _targetText; set => SetField(ref _targetText, value); }
    }

    public class CalendarDayItem : ViewModelBase
    {
        private int _dayNumber;
        private DateTime _date;
        private bool _isCurrentMonth;
        private double _gainedHours;
        private double _timeOffUsed;

        public int DayNumber
        {
            get => _dayNumber;
            set => SetField(ref _dayNumber, value);
        }

        public DateTime Date
        {
            get => _date;
            set => SetField(ref _date, value);
        }

        public bool IsCurrentMonth
        {
            get => _isCurrentMonth;
            set => SetField(ref _isCurrentMonth, value);
        }

        public double GainedHours
        {
            get => _gainedHours;
            set
            {
                if (SetField(ref _gainedHours, value))
                {
                    OnPropertyChanged(nameof(GainedText));
                    OnPropertyChanged(nameof(HasGained));
                    OnPropertyChanged(nameof(DisplayText));
                }
            }
        }

        /// <summary>Hours of flex balance consumed on this day (time off used from savings).</summary>
        public double TimeOffUsed
        {
            get => _timeOffUsed;
            set
            {
                if (SetField(ref _timeOffUsed, value))
                {
                    OnPropertyChanged(nameof(TimeOffUsedText));
                    OnPropertyChanged(nameof(HasTimeOffUsed));
                    OnPropertyChanged(nameof(DisplayText));
                }
            }
        }

        public double SickHours
        {
            get => _sickHours;
            set
            {
                if (SetField(ref _sickHours, value))
                {
                    OnPropertyChanged(nameof(SickDayText));
                    OnPropertyChanged(nameof(HasSickDay));
                }
            }
        }
        private double _sickHours;

        public string SickDayText => SickHours > 0.001 ? $"🤒 {SickHours:F1}h" : "";
        public bool   HasSickDay  => SickHours > 0.001;

        public string GainedText    => GainedHours   > 0.001 ? $"+{GainedHours:F2}h"  : "";
        public string TimeOffUsedText => TimeOffUsed > 0.001 ? $"-{TimeOffUsed:F2}h" : "";
        public bool HasGained       => GainedHours   > 0.001;
        public bool HasTimeOffUsed  => TimeOffUsed   > 0.001;

        /// <summary>Convenience text combining both signals for a cell.</summary>
        public string DisplayText
        {
            get
            {
                if (HasGained && HasTimeOffUsed)  return $"+{GainedHours:F2}h\n-{TimeOffUsed:F2}h";
                if (HasGained)                    return $"+{GainedHours:F2}h";
                if (HasTimeOffUsed)               return $"-{TimeOffUsed:F2}h";
                return "";
            }
        }

        private string _holidayText = string.Empty;
        public string HolidayText
        {
            get => _holidayText;
            set
            {
                if (SetField(ref _holidayText, value))
                {
                    OnPropertyChanged(nameof(HasHoliday));
                }
            }
        }
        public bool HasHoliday => !string.IsNullOrEmpty(HolidayText);
    }

    public class MainViewModel : ViewModelBase
    {
        private string _currentTab = "Dashboard";
        private DateTime _selectedDate = DateTime.Today;
        private DateTime _currentWeekStart = OvertimeCalculator.GetStartOfWeek(DateTime.Today);
        private DateTime _calendarMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        private DateTime _lastToday = DateTime.Today;
        private string _originalProcessName = string.Empty;
        
        // Timeline block editing fields
        private TimelineBlock? _selectedTimelineBlock;
        private string _selectedBlockCategory = "Work";
        private string _selectedBlockDescription = string.Empty;
        
        // Stats properties
        private double _weeklyTargetHours;
        private double _weeklyHoursWorked;
        private double _weeklyTimeOffCredit;
        private double _weeklyNetOvertime;
        private double _weeklyOvertimeEarned;
        private double _lifetimeOvertimeBalance;

        // Custom Schedule Targets
        private double _targetMon = 8.0;
        private double _targetTue = 8.0;
        private double _targetWed = 8.0;
        private double _targetThu = 8.0;
        private double _targetFri = 8.0;
        private double _targetSat = 0.0;
        private double _targetSun = 0.0;
        private bool _isFocusMode = false;
        
        // List properties
        private ObservableCollection<TimelineBlock>      _timeline       = new();
        private ObservableCollection<TopAppViewModel>    _topApps        = new();
        private ObservableCollection<AppCategory>        _appCategories  = new();
        private ObservableCollection<TimeOffLog>         _timeOffDays    = new();
        private ObservableCollection<DailyEarningsItem>  _weeklyEarnings = new();

        // ICS calendar integration
        private string _icsCalendarUrl  = string.Empty;
        private string _icsSyncStatus   = string.Empty;
        private DateTime? _icsMinImportDate;

        public DateTime? IcsMinImportDate
        {
            get => _icsMinImportDate;
            set => SetField(ref _icsMinImportDate, value);
        }

        public ObservableCollection<DailyEventItem> DailyEvents { get; } = new();

        // Holiday properties
        private DateTime _holidayStartDate = DateTime.Today;
        private DateTime _holidayEndDate = DateTime.Today;
        private string _holidayNote = string.Empty;
        private ObservableCollection<HolidayLog> _holidayLogs = new();

        public DateTime HolidayStartDate
        {
            get => _holidayStartDate;
            set => SetField(ref _holidayStartDate, value);
        }

        public DateTime HolidayEndDate
        {
            get => _holidayEndDate;
            set => SetField(ref _holidayEndDate, value);
        }

        public string HolidayNote
        {
            get => _holidayNote;
            set => SetField(ref _holidayNote, value);
        }

        public ObservableCollection<HolidayLog> HolidayLogs
        {
            get => _holidayLogs;
            set => SetField(ref _holidayLogs, value);
        }

        public string HolidayBalanceText
        {
            get
            {
                int used = GetUsedHolidayDaysForYear(DateTime.Today.Year, HolidayLogs.Where(h => !h.IsPublicHoliday).ToList());
                return $"{25 - used} days left";
            }
        }

        // Selected Category editing
        private string _selectedProcessName = string.Empty;
        private string _selectedCategoryValue = "Work";

        // Navigation Command
        public ICommand SetTabCommand { get; }
        public ICommand SetSettingsTabCommand { get; }
        public ICommand SaveScheduleCommand { get; }
        
        // Date/Week Command
        public ICommand PrevDayCommand { get; }
        public ICommand NextDayCommand { get; }
        public ICommand TodayCommand { get; }
        
        public ICommand PrevWeekCommand { get; }
        public ICommand NextWeekCommand  { get; }
        public ICommand ThisWeekCommand  { get; }
        
        // Settings Command
        public ICommand SaveCategoryCommand { get; }
        public ICommand DeleteCategoryCommand { get; }
        
        // Time Off Command
        public ICommand ToggleTimeOffCommand { get; }

        // Timeline block editing command
        public ICommand SaveTimelineBlockCommand { get; }

        // Calendar View Commands & Collection
        public ICommand PrevMonthCommand { get; }
        public ICommand NextMonthCommand { get; }
        public ObservableCollection<CalendarDayItem> CalendarDays { get; } = new();

        // ICS Integration
        public ICommand SaveIcsUrlCommand { get; }
        public ICommand SyncIcsNowCommand { get; }

        // Holiday Commands
        public ICommand RegisterHolidayCommand { get; }
        public ICommand DeleteHolidayCommand { get; }

        public string CurrentTab
        {
            get => _currentTab;
            set => SetField(ref _currentTab, value);
        }

        private string _currentSettingsTab = "App Rules";
        public string CurrentSettingsTab
        {
            get => _currentSettingsTab;
            set => SetField(ref _currentSettingsTab, value);
        }

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (SetField(ref _selectedDate, value))
                {
                    OnPropertyChanged(nameof(SelectedDateText));
                    RefreshDayData();
                }
            }
        }

        public DateTime CurrentWeekStart
        {
            get => _currentWeekStart;
            set
            {
                if (SetField(ref _currentWeekStart, value))
                {
                    OnPropertyChanged(nameof(SelectedWeekText));
                    RefreshWeekData();
                }
            }
        }

        public string SelectedDateText => SelectedDate.ToString("dddd, MMMM dd, yyyy");
        public string SelectedWeekText => $"Week of {CurrentWeekStart:MMMM dd, yyyy} (Target: {WeeklyTargetHours:F1} hrs)";

        #region Stats Bindings
        public double WeeklyTargetHours
        {
            get => _weeklyTargetHours;
            set
            {
                if (SetField(ref _weeklyTargetHours, value))
                {
                    OnPropertyChanged(nameof(SelectedWeekText));
                    OnPropertyChanged(nameof(WeeklyProgressPercent));
                    OnPropertyChanged(nameof(WeeklyHoursText));
                }
            }
        }

        public double WeeklyHoursWorked
        {
            get => _weeklyHoursWorked;
            set
            {
                if (SetField(ref _weeklyHoursWorked, value))
                {
                    OnPropertyChanged(nameof(WeeklyProgressPercent));
                    OnPropertyChanged(nameof(WeeklyHoursText));
                }
            }
        }

        public double WeeklyTimeOffCredit
        {
            get => _weeklyTimeOffCredit;
            set
            {
                if (SetField(ref _weeklyTimeOffCredit, value))
                {
                    OnPropertyChanged(nameof(WeeklyProgressPercent));
                    OnPropertyChanged(nameof(WeeklyHoursText));
                }
            }
        }

        public double WeeklyNetOvertime
        {
            get => _weeklyNetOvertime;
            set => SetField(ref _weeklyNetOvertime, value);
        }

        public double WeeklyOvertimeEarned
        {
            get => _weeklyOvertimeEarned;
            set => SetField(ref _weeklyOvertimeEarned, value);
        }

        public double LifetimeOvertimeBalance
        {
            get => _lifetimeOvertimeBalance;
            set
            {
                if (SetField(ref _lifetimeOvertimeBalance, value))
                    OnPropertyChanged(nameof(LifetimeOvertimeText));
            }
        }

        public double WeeklyProgressPercent
        {
            get
            {
                if (WeeklyTargetHours <= 0) return 100;
                double effectiveWorked = WeeklyHoursWorked + WeeklyTimeOffCredit;
                return Math.Min(100, (effectiveWorked / WeeklyTargetHours) * 100);
            }
        }

        public string WeeklyHoursText => $"{WeeklyHoursWorked:F1}h / {WeeklyTargetHours:F1}h";
        
        public string LifetimeOvertimeText
        {
            get
            {
                string sign = LifetimeOvertimeBalance >= 0 ? "+" : "";
                return $"{sign}{LifetimeOvertimeBalance:F2} hrs";
            }
        }

        public double TargetMon
        {
            get => _targetMon;
            set => SetField(ref _targetMon, value);
        }

        public double TargetTue
        {
            get => _targetTue;
            set => SetField(ref _targetTue, value);
        }

        public double TargetWed
        {
            get => _targetWed;
            set => SetField(ref _targetWed, value);
        }

        public double TargetThu
        {
            get => _targetThu;
            set => SetField(ref _targetThu, value);
        }

        public double TargetFri
        {
            get => _targetFri;
            set => SetField(ref _targetFri, value);
        }

        public double TargetSat
        {
            get => _targetSat;
            set => SetField(ref _targetSat, value);
        }

        public double TargetSun
        {
            get => _targetSun;
            set => SetField(ref _targetSun, value);
        }

        public bool IsFocusMode
        {
            get => _isFocusMode;
            set
            {
                if (SetField(ref _isFocusMode, value))
                {
                    try
                    {
                        using var db = new DatabaseContext();
                        var setting = db.AppSettings.Find("FocusModeEnabled");
                        string valStr = value ? "true" : "false";
                        if (setting != null)
                        {
                            setting.Value = valStr;
                        }
                        else
                        {
                            db.AppSettings.Add(new AppSetting { Key = "FocusModeEnabled", Value = valStr });
                        }
                        db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error saving focus mode setting: {ex.Message}");
                    }
                }
            }
        }

        private ObservableCollection<DailyChartItem> _weeklyChartData = new();
        public ObservableCollection<DailyChartItem> WeeklyChartData
        {
            get => _weeklyChartData;
            set => SetField(ref _weeklyChartData, value);
        }
        #endregion

        #region Collection Bindings
        public ObservableCollection<TimelineBlock> Timeline
        {
            get => _timeline;
            set => SetField(ref _timeline, value);
        }

        public ObservableCollection<TopAppViewModel> TopApps
        {
            get => _topApps;
            set => SetField(ref _topApps, value);
        }

        public ObservableCollection<AppCategory> AppCategories
        {
            get => _appCategories;
            set => SetField(ref _appCategories, value);
        }

        public ObservableCollection<TimeOffLog> TimeOffDays
        {
            get => _timeOffDays;
            set => SetField(ref _timeOffDays, value);
        }

        public ObservableCollection<DailyEarningsItem> WeeklyEarnings
        {
            get => _weeklyEarnings;
            set => SetField(ref _weeklyEarnings, value);
        }

        public string SelectedProcessName
        {
            get => _selectedProcessName;
            set => SetField(ref _selectedProcessName, value);
        }

        public string SelectedCategoryValue
        {
            get => _selectedCategoryValue;
            set => SetField(ref _selectedCategoryValue, value);
        }

        private AppCategory? _selectedAppCategory;
        public AppCategory? SelectedAppCategory
        {
            get => _selectedAppCategory;
            set
            {
                if (SetField(ref _selectedAppCategory, value))
                {
                    if (value != null)
                    {
                        SelectedProcessName = value.ProcessName;
                        SelectedCategoryValue = value.CategoryName;
                        _originalProcessName = value.ProcessName;
                    }
                }
            }
        }

        public DateTime CalendarMonth
        {
            get => _calendarMonth;
            set
            {
                if (SetField(ref _calendarMonth, value))
                {
                    OnPropertyChanged(nameof(CalendarMonthText));
                    RefreshCalendarView();
                }
            }
        }

        public string CalendarMonthText => CalendarMonth.ToString("MMMM yyyy");

        public double UnlinkedTimeOffTotal
        {
            get
            {
                try
                {
                    using var db = new DatabaseContext();
                    var timeOffs = db.TimeOffLogs.ToList();
                    double totalUnlinkedGained = timeOffs.Where(t => t.Date.Year == 1900).Sum(t => t.Hours);
                    double usedFlexHours = timeOffs.Where(t => t.Date.Year != 1900 && t.Type != "Holiday").Sum(t => t.Hours);
                    return Math.Max(0, totalUnlinkedGained - usedFlexHours);
                }
                catch
                {
                    return 0;
                }
            }
        }

        // Selected timeline block properties
        public TimelineBlock? SelectedTimelineBlock
        {
            get => _selectedTimelineBlock;
            set
            {
                if (SetField(ref _selectedTimelineBlock, value))
                {
                    if (value != null)
                    {
                        SelectedBlockCategory = value.Category;
                        string details = value.AppDetails;
                        if (details.Contains(" (Active:"))
                        {
                            int index = details.IndexOf(" (Active:");
                            SelectedBlockDescription = index > 0 ? details.Substring(0, index) : details;
                        }
                        else
                        {
                            SelectedBlockDescription = details == "Away / Inactive" ? "" : details;
                        }
                    }
                    OnPropertyChanged(nameof(IsBlockSelected));
                }
            }
        }

        public string SelectedBlockCategory
        {
            get => _selectedBlockCategory;
            set => SetField(ref _selectedBlockCategory, value);
        }

        public string SelectedBlockDescription
        {
            get => _selectedBlockDescription;
            set => SetField(ref _selectedBlockDescription, value);
        }

        public bool IsBlockSelected => SelectedTimelineBlock != null;
        #endregion

        public MainViewModel()
        {
            // Set up commands
            SetTabCommand = new RelayCommand<string>(tab => CurrentTab = tab ?? "Dashboard");
            SetSettingsTabCommand = new RelayCommand<string>(tab => CurrentSettingsTab = tab ?? "App Rules");
            
            PrevDayCommand = new RelayCommand(() => SelectedDate = SelectedDate.AddDays(-1));
            NextDayCommand = new RelayCommand(() => SelectedDate = SelectedDate.AddDays(1));
            TodayCommand = new RelayCommand(() => SelectedDate = DateTime.Today);
            
            PrevWeekCommand  = new RelayCommand(() => CurrentWeekStart = CurrentWeekStart.AddDays(-7));
            NextWeekCommand  = new RelayCommand(() => CurrentWeekStart = CurrentWeekStart.AddDays(7));
            ThisWeekCommand  = new RelayCommand(() => CurrentWeekStart = OvertimeCalculator.GetStartOfWeek(DateTime.Today));
            
            SaveCategoryCommand   = new RelayCommand(SaveCategoryRule);
            DeleteCategoryCommand = new RelayCommand(DeleteCategoryRule);
            ToggleTimeOffCommand  = new RelayCommand<WeekdayItem>(ToggleTimeOffDay);
            SaveTimelineBlockCommand = new RelayCommand(SaveTimelineBlockChanges);
            SaveIcsUrlCommand     = new RelayCommand(SaveIcsUrl);
            SyncIcsNowCommand     = new RelayCommand(SyncIcsNow);
            RegisterHolidayCommand = new RelayCommand(RegisterHoliday);
            DeleteHolidayCommand = new RelayCommand<HolidayLog>(DeleteHoliday);
            SaveScheduleCommand   = new RelayCommand(SaveScheduleSettings);

            // Load persisted ICS settings from DB
            try
            {
                using var db = new DatabaseContext();
                _icsCalendarUrl = db.AppSettings.Find("IcsCalendarUrl")?.Value ?? string.Empty;
                
                string minDateStr = db.AppSettings.Find("IcsMinImportDate")?.Value ?? string.Empty;
                if (DateTime.TryParse(minDateStr, out DateTime minDate))
                {
                    _icsMinImportDate = minDate;
                }

                _icsSyncStatus  = string.IsNullOrEmpty(_icsCalendarUrl)
                    ? "No calendar URL configured."
                    : "Settings loaded. Sync runs every hour.";
            }
            catch { }

            PrevMonthCommand = new RelayCommand(() => CalendarMonth = CalendarMonth.AddMonths(-1));
            NextMonthCommand = new RelayCommand(() => CalendarMonth = CalendarMonth.AddMonths(1));
            
            LoadScheduleSettings();
            RefreshHolidayLogsList();
            RefreshData();

            // Setup a timer to check if the day rolled over (e.g. crossing midnight) and auto-advance views
            var midnightTimer = new System.Windows.Threading.DispatcherTimer();
            midnightTimer.Interval = TimeSpan.FromSeconds(30);
            midnightTimer.Tick += (s, e) =>
            {
                if (DateTime.Today != _lastToday)
                {
                    DateTime oldToday = _lastToday;
                    _lastToday = DateTime.Today;

                    bool changed = false;
                    if (SelectedDate == oldToday)
                    {
                        SelectedDate = DateTime.Today;
                        changed = true;
                    }
                    if (CurrentWeekStart == OvertimeCalculator.GetStartOfWeek(oldToday))
                    {
                        CurrentWeekStart = OvertimeCalculator.GetStartOfWeek(DateTime.Today);
                        changed = true;
                    }
                    if (CalendarMonth.Year == oldToday.Year && CalendarMonth.Month == oldToday.Month)
                    {
                        CalendarMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                        changed = true;
                    }

                    if (!changed)
                    {
                        RefreshData();
                    }
                }
            };
            midnightTimer.Start();
        }

        private void LoadScheduleSettings()
        {
            try
            {
                using var db = new DatabaseContext();
                var settings = db.AppSettings.ToList();

                _targetMon = GetScheduleSetting(settings, "Schedule_Monday", 8.0);
                _targetTue = GetScheduleSetting(settings, "Schedule_Tuesday", 8.0);
                _targetWed = GetScheduleSetting(settings, "Schedule_Wednesday", 8.0);
                _targetThu = GetScheduleSetting(settings, "Schedule_Thursday", 8.0);
                _targetFri = GetScheduleSetting(settings, "Schedule_Friday", 8.0);
                _targetSat = GetScheduleSetting(settings, "Schedule_Saturday", 0.0);
                _targetSun = GetScheduleSetting(settings, "Schedule_Sunday", 0.0);

                var focusSetting = settings.FirstOrDefault(s => s.Key == "FocusModeEnabled");
                _isFocusMode = focusSetting != null && focusSetting.Value == "true";
            }
            catch
            {
                _targetMon = 8.0; _targetTue = 8.0; _targetWed = 8.0; _targetThu = 8.0; _targetFri = 8.0;
                _targetSat = 0.0; _targetSun = 0.0;
                _isFocusMode = false;
            }
        }

        private double GetScheduleSetting(List<AppSetting> settings, string key, double defaultVal)
        {
            var setting = settings.FirstOrDefault(s => s.Key == key);
            if (setting != null && double.TryParse(setting.Value.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double val))
            {
                return val;
            }
            return defaultVal;
        }

        private void SaveScheduleSettings()
        {
            try
            {
                using var db = new DatabaseContext();
                SaveSetting(db, "Schedule_Monday", TargetMon);
                SaveSetting(db, "Schedule_Tuesday", TargetTue);
                SaveSetting(db, "Schedule_Wednesday", TargetWed);
                SaveSetting(db, "Schedule_Thursday", TargetThu);
                SaveSetting(db, "Schedule_Friday", TargetFri);
                SaveSetting(db, "Schedule_Saturday", TargetSat);
                SaveSetting(db, "Schedule_Sunday", TargetSun);
                db.SaveChanges();

                RefreshData();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving schedule settings: {ex.Message}");
            }
        }

        private void SaveSetting(DatabaseContext db, string key, double value)
        {
            var setting = db.AppSettings.Find(key);
            string valStr = value.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
            if (setting != null)
            {
                setting.Value = valStr;
            }
            else
            {
                db.AppSettings.Add(new AppSetting { Key = key, Value = valStr });
            }
        }

        public void RefreshData()
        {
            RefreshWeekData();
            RefreshDayData();
            RefreshCategories();
            RefreshCalendarView();
            OnPropertyChanged(nameof(UnlinkedTimeOffTotal));
        }

        private void RefreshWeekData()
        {
            try
            {
                using var db = new DatabaseContext();
                var logs = db.AppUsageLogs.ToList();
                var timeOffs = GetCombinedTimeOffLogs(db);

                // Compute weekly stats
                var weekResult = OvertimeCalculator.Calculate(CurrentWeekStart, logs, timeOffs);
                WeeklyTargetHours = weekResult.TargetWorkHours;
                WeeklyHoursWorked = weekResult.WeekdayHoursWorked;
                WeeklyTimeOffCredit = weekResult.TimeOffHoursCredit;
                WeeklyNetOvertime = weekResult.NetWeekdayOvertimeHours;
                WeeklyOvertimeEarned = weekResult.TotalOvertimeTimeOffEarned;

                // Compute remaining unlinked time off
                double totalUnlinkedGained = timeOffs.Where(t => t.Date.Year == 1900).Sum(t => t.Hours);
                double usedFlexHours = timeOffs.Where(t => t.Date.Year != 1900 && t.Type != "Holiday").Sum(t => t.Hours);
                double remainingUnlinkedHours = Math.Max(0, totalUnlinkedGained - usedFlexHours);
                LifetimeOvertimeBalance = CalculateLifetimeOvertime(logs, timeOffs) + remainingUnlinkedHours;

                // Populate active week's time-off days
                DateTime weekEnd = CurrentWeekStart.AddDays(7);
                var weekTimeOffLogs = timeOffs
                    .Where(t => t.Date >= CurrentWeekStart && t.Date < weekEnd)
                    .OrderBy(t => t.Date)
                    .ToList();
                
                TimeOffDays.Clear();
                foreach (var log in weekTimeOffLogs)
                {
                    TimeOffDays.Add(log);
                }

                // Populate daily worked vs target chart data
                var chartItems = new List<DailyChartItem>();
                for (int i = 0; i < 7; i++)
                {
                    DateTime day = CurrentWeekStart.AddDays(i);
                    DateTime limitDate = day.AddDays(1);

                    var dayLogs = logs.Where(l => l.StartTime >= day && l.StartTime < limitDate).ToList();
                    var cutoff = OvertimeCalculator.IsUnitTest ? DateTime.MaxValue : DateTime.Now;
                    var roundedDayLogs = OvertimeCalculator.ApplyRounding(dayLogs.Where(l => l.StartTime <= cutoff).ToList());
                    var activeDayLogs = roundedDayLogs
                        .Where(l => OvertimeCalculator.IsWorkCategory(l.Category) && l.ProcessName != "sick_time")
                        .ToList();
                    var mergedDayLogs = OvertimeCalculator.MergeOverlappingLogs(activeDayLogs);
                    double dayHoursWorked = mergedDayLogs.Sum(l => (l.EndTime - l.StartTime).TotalHours);

                    double dayTimeOff = timeOffs.FirstOrDefault(t => t.Date.Date == day.Date)?.Hours ?? 0;
                    double daySickHours = logs
                        .Where(l => l.ProcessName == "sick_time" && l.StartTime.Date == day.Date)
                        .Sum(l => (l.EndTime - l.StartTime).TotalHours);

                    double baseDayTarget = i == 0 ? TargetMon :
                                           i == 1 ? TargetTue :
                                           i == 2 ? TargetWed :
                                           i == 3 ? TargetThu :
                                           i == 4 ? TargetFri :
                                           i == 5 ? TargetSat : TargetSun;

                    double dayTarget = Math.Max(0, baseDayTarget - dayTimeOff - daySickHours);

                    if ((day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday) && dayHoursWorked == 0 && dayTarget == 0)
                    {
                        continue;
                    }

                    double standard = Math.Min(dayHoursWorked, dayTarget);
                    double overtime = Math.Max(0, dayHoursWorked - dayTarget);
                    double remaining = Math.Max(0, dayTarget - dayHoursWorked);

                    chartItems.Add(new DailyChartItem
                    {
                        DayName = day.ToString("ddd"), // "Mon", "Tue" etc.
                        StandardHours = standard,
                        OvertimeHours = overtime,
                        RemainingTarget = remaining,
                        WorkedText = $"{dayHoursWorked:F1}h",
                        TargetText = $"{dayTarget:F1}h target"
                    });
                }

                WeeklyChartData.Clear();
                foreach (var item in chartItems)
                {
                    WeeklyChartData.Add(item);
                }

                // Populate weekly earnings page daily breakdown
                RefreshWeeklyEarnings(logs, timeOffs);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading weekly data: {ex.Message}");
            }
        }

        private void RefreshWeeklyEarnings(List<AppUsageLog> logs, List<TimeOffLog> timeOffs)
        {
            try
            {
                var list = new List<DailyEarningsItem>();
                double previousCumulative = 0;

                for (int i = 0; i < 7; i++)
                {
                    DateTime day = CurrentWeekStart.AddDays(i);
                    DateTime limitDate = day.AddDays(1);

                    // Filter logs and time-offs up to the end of this day
                    var logsUpToDay = logs
                        .Where(l => l.StartTime >= CurrentWeekStart && l.EndTime <= limitDate)
                        .ToList();

                    var timeOffsUpToDay = timeOffs
                        .Where(t => t.Date >= CurrentWeekStart && t.Date < limitDate)
                        .ToList();

                    // Calculate calculations up to this day of the week
                    var calcResult = OvertimeCalculator.Calculate(CurrentWeekStart, logsUpToDay, timeOffsUpToDay);

                    // Filter active logs for just this day
                    var dayLogs = logs
                        .Where(l => l.StartTime >= day && l.StartTime < limitDate)
                        .ToList();

                    // Apply rounding and future cutoff just like OvertimeCalculator
                    var cutoff = OvertimeCalculator.IsUnitTest ? DateTime.MaxValue : DateTime.Now;
                    var roundedDayLogs = OvertimeCalculator.ApplyRounding(
                        dayLogs.Where(l => l.StartTime <= cutoff).ToList()
                    );

                    var activeDayLogs = roundedDayLogs
                        .Where(l => OvertimeCalculator.IsWorkCategory(l.Category) && l.ProcessName != "sick_time")
                        .ToList();

                    // Merge overlapping work/meeting logs so we don't double count overlaps/all-day events
                    var mergedDayLogs = OvertimeCalculator.MergeOverlappingLogs(activeDayLogs);
                    double dayHours = mergedDayLogs.Sum(l => (l.EndTime - l.StartTime).TotalHours);

                    double dayTimeOff = timeOffs.FirstOrDefault(t => t.Date.Date == day.Date)?.Hours ?? 0;
                    double daySickHours = logs
                        .Where(l => l.ProcessName == "sick_time" && l.StartTime.Date == day.Date)
                        .Sum(l => (l.EndTime - l.StartTime).TotalHours);

                    double baseDayTarget = i == 0 ? TargetMon :
                                           i == 1 ? TargetTue :
                                           i == 2 ? TargetWed :
                                           i == 3 ? TargetThu :
                                           i == 4 ? TargetFri :
                                           i == 5 ? TargetSat : TargetSun;

                    double dayTarget = Math.Max(0, baseDayTarget - dayTimeOff - daySickHours);

                    double cumulative = calcResult.TotalOvertimeTimeOffEarned;
                    double gained = cumulative - previousCumulative;
                    previousCumulative = cumulative;

                    list.Add(new DailyEarningsItem
                    {
                        DayName = day.ToString("dddd"),
                        DateText = day.ToString("MMMM dd, yyyy"),
                        Date = day,
                        HoursWorked = dayHours,
                        TargetHours = dayTarget,
                        TimeOffCredit = dayTimeOff,
                        GainedTimeOff = gained,
                        SavedUpTimeOff = cumulative
                    });
                }

                WeeklyEarnings.Clear();
                foreach (var item in list)
                {
                    WeeklyEarnings.Add(item);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating weekly earnings: {ex.Message}");
            }
        }

        private void RefreshDayData()
        {
            try
            {
                using var db = new DatabaseContext();
                
                DateTime startOfDay = SelectedDate.Date;
                DateTime endOfDay = startOfDay.AddDays(1);

                var dbLogs = db.AppUsageLogs
                    .Where(l => l.StartTime >= startOfDay && l.StartTime < endOfDay)
                    .ToList();

                var combinedTimeOffs = GetCombinedTimeOffLogs(db);
                var dayTimeOff = combinedTimeOffs.FirstOrDefault(t => t.Date.Date == startOfDay.Date);

                var dayLogs = OvertimeCalculator.ApplyRounding(dbLogs);

                // 1. Generate Daily App Usage Timeline (30 min blocks)
                var blocks = new List<TimelineBlock>();
                for (int h = 0; h < 24; h++)
                {
                    for (int m = 0; m < 60; m += 30)
                    {
                        DateTime blockStart = startOfDay.AddHours(h).AddMinutes(m);
                        DateTime blockEnd = blockStart.AddMinutes(30);

                        var overlappingLogs = dayLogs
                            .Where(l => l.StartTime < blockEnd && l.EndTime > blockStart)
                            .ToList();

                        var localAppLogs = overlappingLogs
                            .Where(l => l.ProcessName != "ics_import" && l.ProcessName != "outlook_import" && l.ProcessName != "sick_time" && l.ProcessName != "rounded_time" && l.Category != "Ignore")
                            .ToList();

                        var blockCalendarLogs = overlappingLogs
                            .Where(l => l.ProcessName == "ics_import" || l.ProcessName == "outlook_import")
                            .GroupBy(l => new { l.StartTime, l.EndTime, Title = (l.WindowTitle ?? "").Trim() })
                            .Select(g => g.First())
                            .ToList();

                        double localActiveSeconds = 0;
                        var localCatDurations = new Dictionary<string, double>();
                        var localProcessDurations = new Dictionary<string, double>();

                        foreach (var log in localAppLogs)
                        {
                            DateTime overlapStart = log.StartTime < blockStart ? blockStart : log.StartTime;
                            DateTime overlapEnd = log.EndTime > blockEnd ? blockEnd : log.EndTime;
                            double overlapSec = (overlapEnd - overlapStart).TotalSeconds;
                            if (overlapSec > 0)
                            {
                                localActiveSeconds += overlapSec;
                                localCatDurations[log.Category] = localCatDurations.GetValueOrDefault(log.Category) + overlapSec;
                                localProcessDurations[log.ProcessName] = localProcessDurations.GetValueOrDefault(log.ProcessName) + overlapSec;
                            }
                        }

                        string dominantCategory = "Away";
                        string appDetails = "Away / Inactive";
                        double activePercentage = 0;

                        if (localActiveSeconds > 30) // WorkTracker app tracking dominates
                        {
                            dominantCategory = localCatDurations.OrderByDescending(kv => kv.Value).First().Key;
                            var dominantProcess = localProcessDurations.OrderByDescending(kv => kv.Value).First().Key;

                            if (dominantProcess == "offline")
                            {
                                var offlineLog = localAppLogs
                                    .Where(l => l.ProcessName == "offline" && !string.IsNullOrWhiteSpace(l.WindowTitle))
                                    .OrderByDescending(l => (l.EndTime < blockEnd ? l.EndTime : blockEnd) - (l.StartTime > blockStart ? l.StartTime : blockStart))
                                    .FirstOrDefault();
                                string desc = offlineLog?.WindowTitle ?? dominantCategory;
                                appDetails = $"{desc} ({(localActiveSeconds / 60.0):F0} mins)";
                            }
                            else
                            {
                                string appName = string.IsNullOrEmpty(dominantProcess) ? "Unknown" : char.ToUpper(dominantProcess[0]) + dominantProcess.Substring(1);
                                appDetails = $"{appName} (Active: {(localActiveSeconds / 60.0):F0} mins)";
                            }
                            activePercentage = (localActiveSeconds / 1800.0) * 100;
                        }
                        else if (blockCalendarLogs.Count > 0) // No local app tracking, check calendar
                        {
                            double calActiveSeconds = 0;
                            var calCatDurations = new Dictionary<string, double>();
                            var calProcessDurations = new Dictionary<string, double>();

                            foreach (var log in blockCalendarLogs)
                            {
                                DateTime overlapStart = log.StartTime < blockStart ? blockStart : log.StartTime;
                                DateTime overlapEnd = log.EndTime > blockEnd ? blockEnd : log.EndTime;
                                double overlapSec = (overlapEnd - overlapStart).TotalSeconds;
                                if (overlapSec > 0)
                                {
                                    calActiveSeconds += overlapSec;
                                    calCatDurations[log.Category] = calCatDurations.GetValueOrDefault(log.Category) + overlapSec;
                                    calProcessDurations[log.ProcessName] = calProcessDurations.GetValueOrDefault(log.ProcessName) + overlapSec;
                                }
                            }

                            if (calActiveSeconds > 0)
                            {
                                dominantCategory = calCatDurations.OrderByDescending(kv => kv.Value).First().Key;
                                var dominantProcess = calProcessDurations.OrderByDescending(kv => kv.Value).First().Key;

                                var meetingLog = blockCalendarLogs
                                    .Where(l => !string.IsNullOrWhiteSpace(l.WindowTitle))
                                    .OrderByDescending(l => (l.EndTime < blockEnd ? l.EndTime : blockEnd) - (l.StartTime > blockStart ? l.StartTime : blockStart))
                                    .FirstOrDefault();
                                string meetingTitle = meetingLog?.WindowTitle ?? "Meeting";
                                appDetails = $"📅 {meetingTitle} ({(calActiveSeconds / 60.0):F0} mins)";
                                activePercentage = (calActiveSeconds / 1800.0) * 100;
                            }
                        }
                        else if (dayTimeOff != null && (h >= 8 && h < 16)) // Time off / Holiday timeline rendering
                        {
                            dominantCategory = dayTimeOff.Type; // "Vacation", "Holiday", "Sick Day"
                            string icon = dayTimeOff.Type == "Holiday" ? "🌴" : (dayTimeOff.Type == "Sick Day" ? "🤒" : "🌴");
                            appDetails = $"{icon} {dayTimeOff.Type} (Planned)";
                            activePercentage = 100;
                        }

                        bool isWeekend = SelectedDate.DayOfWeek == DayOfWeek.Saturday || SelectedDate.DayOfWeek == DayOfWeek.Sunday;
                        bool isDefaultWorkHours = !isWeekend && (h >= 8 && h < 16);
                        bool isActive = (localActiveSeconds > 30) || (dominantCategory != "Away");

                        if (isDefaultWorkHours || isActive)
                        {
                            blocks.Add(new TimelineBlock
                            {
                                TimeLabel = $"{blockStart:HH:mm} - {blockEnd:HH:mm}",
                                Category = dominantCategory,
                                AppDetails = appDetails,
                                ActivePercentage = activePercentage
                            });
                        }
                    }
                }

                Timeline.Clear();
                foreach (var b in blocks)
                {
                    Timeline.Add(b);
                }

                // Generate Daily Calendar Events list
                var calendarLogs = dbLogs
                    .Where(l => l.ProcessName == "ics_import" || l.ProcessName == "outlook_import")
                    .GroupBy(l => new { l.StartTime, l.EndTime, Title = (l.WindowTitle ?? "").Trim() })
                    .Select(g => g.First())
                    .OrderBy(l => l.StartTime)
                    .Select(l => new DailyEventItem
                    {
                        Title = l.WindowTitle,
                        TimeLabel = $"{l.StartTime:HH:mm} - {l.EndTime:HH:mm}",
                        Category = l.Category
                    })
                    .ToList();

                DailyEvents.Clear();
                foreach (var evt in calendarLogs)
                {
                    DailyEvents.Add(evt);
                }

                // 2. Generate Daily Top Apps breakdown
                var appGroups = dayLogs
                    .Where(l => l.Category != "Ignore" && l.ProcessName != "rounded_time"
                             && l.ProcessName != "sick_time"
                             && l.ProcessName != "outlook_import"
                             && l.ProcessName != "ics_import")
                    .GroupBy(l => new { l.ProcessName, l.Category })
                    .Select(g => new TopAppViewModel
                    {
                        ProcessName = char.ToUpper(g.Key.ProcessName[0]) + g.Key.ProcessName.Substring(1),
                        Category = g.Key.Category,
                        DurationHours = g.Sum(l => (l.EndTime - l.StartTime).TotalHours)
                    })
                    .OrderByDescending(a => a.DurationHours)
                    .ToList();

                TopApps.Clear();
                foreach (var app in appGroups)
                {
                    app.DurationText = app.DurationHours >= 1.0 
                        ? $"{app.DurationHours:F1} hrs" 
                        : $"{(app.DurationHours * 60):F0} mins";
                    TopApps.Add(app);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading daily data: {ex.Message}");
            }
        }

        private void RefreshCategories()
        {
            try
            {
                using var db = new DatabaseContext();
                var categories = db.AppCategories.OrderBy(c => c.ProcessName).ToList();

                AppCategories.Clear();
                foreach (var cat in categories)
                {
                    AppCategories.Add(cat);
                }

                if (string.IsNullOrEmpty(SelectedProcessName) && categories.Count > 0)
                {
                    SelectedAppCategory = categories[0];
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading categories: {ex.Message}");
            }
        }

        private double CalculateLifetimeOvertime(List<AppUsageLog> allLogs, List<TimeOffLog> allTimeOffs)
        {
            var firstLog = allLogs.OrderBy(l => l.StartTime).FirstOrDefault();
            if (firstLog == null) return 0;

            DateTime minDate = OvertimeCalculator.GetStartOfWeek(firstLog.StartTime);
            DateTime maxDate = OvertimeCalculator.GetStartOfWeek(DateTime.Now).AddDays(7);

            double balance = 0;
            for (DateTime dt = minDate; dt < maxDate; dt = dt.AddDays(7))
            {
                var result = OvertimeCalculator.Calculate(dt, allLogs, allTimeOffs);
                balance += result.TotalOvertimeTimeOffEarned;
            }

            // Calculate spent flex hours (excluding holidays)
            double usedFlexHours = allTimeOffs
                .Where(t => t.Date.Year != 1900 && t.Type != "Holiday")
                .Sum(t => t.Hours);

            // Calculate total unlinked gained hours
            double totalUnlinkedGained = allTimeOffs
                .Where(t => t.Date.Year == 1900)
                .Sum(t => t.Hours);

            // Remaining spent flex that needs to be subtracted from tracked overtime
            double remainingSpentFlex = Math.Max(0, usedFlexHours - totalUnlinkedGained);
            balance -= remainingSpentFlex;

            return balance;
        }

        private void SaveCategoryRule()
        {
            if (string.IsNullOrWhiteSpace(SelectedProcessName)) return;

            string process = ActivityTracker.CleanProcessName(SelectedProcessName);
            string original = ActivityTracker.CleanProcessName(_originalProcessName);
            try
            {
                using var db = new DatabaseContext();

                // If the process name was renamed, remove the old database entry first
                if (!string.IsNullOrEmpty(original) && process != original)
                {
                    var oldEntry = db.AppCategories.Find(original);
                    if (oldEntry != null)
                    {
                        db.AppCategories.Remove(oldEntry);
                        db.SaveChanges();
                    }

                    // Reset app category cache for the old name to default
                    if (System.Windows.Application.Current is App appInstance)
                    {
                        appInstance.UpdateCategoryCache(original, "Work");
                    }
                }

                var entry = db.AppCategories.Find(process);
                if (entry != null)
                {
                    entry.CategoryName = SelectedCategoryValue;
                    db.SaveChanges();
                }
                else
                {
                    db.AppCategories.Add(new AppCategory
                    {
                        ProcessName = process,
                        CategoryName = SelectedCategoryValue
                    });
                    db.SaveChanges();
                }

                // Update category cache in App instance
                if (System.Windows.Application.Current is App appInst)
                {
                    appInst.UpdateCategoryCache(process, SelectedCategoryValue);
                }

                // Reset selection
                SelectedProcessName = string.Empty;
                _originalProcessName = string.Empty;
                SelectedAppCategory = null;

                RefreshCategories();
                RefreshDayData();
                RefreshWeekData();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving category rule: {ex.Message}");
            }
        }

        // ── ICS Calendar Integration ───────────────────────────────────────────

        public string IcsCalendarUrl
        {
            get => _icsCalendarUrl;
            set => SetField(ref _icsCalendarUrl, value);
        }

        public string IcsSyncStatus
        {
            get => _icsSyncStatus;
            set => SetField(ref _icsSyncStatus, value);
        }

        private void SaveIcsUrl()
        {
            try
            {
                using var db = new DatabaseContext();
                
                // Save ICS URL
                var existingUrl = db.AppSettings.Find("IcsCalendarUrl");
                if (existingUrl != null)
                {
                    existingUrl.Value = IcsCalendarUrl.Trim();
                }
                else
                {
                    db.AppSettings.Add(new AppSetting
                    {
                        Key   = "IcsCalendarUrl",
                        Value = IcsCalendarUrl.Trim()
                    });
                }

                // Save Min Import Date
                var existingMinDate = db.AppSettings.Find("IcsMinImportDate");
                string minDateVal = IcsMinImportDate.HasValue ? IcsMinImportDate.Value.ToString("yyyy-MM-dd") : string.Empty;
                if (existingMinDate != null)
                {
                    existingMinDate.Value = minDateVal;
                }
                else
                {
                    db.AppSettings.Add(new AppSetting
                    {
                        Key   = "IcsMinImportDate",
                        Value = minDateVal
                    });
                }

                db.SaveChanges();
                
                IcsSyncStatus = "✅ Settings saved! Syncing now...";
                SyncIcsNow();
            }
            catch (Exception ex)
            {
                IcsSyncStatus = $"❌ Save failed: {ex.Message}";
                Debug.WriteLine($"[IcsCalendarUrl] Save failed: {ex.Message}");
            }
        }

        private void SyncIcsNow()
        {
            IcsSyncStatus = "🔄 Syncing calendar...";
            if (System.Windows.Application.Current is App appInst)
            {
                appInst.RefreshIcsUrl();
            }
        }

        private void ToggleTimeOffDay(WeekdayItem? item)
        {
            if (item == null) return;

            DateTime date = item.Date.Date;

            try
            {
                using var db = new DatabaseContext();
                var existing = db.TimeOffLogs.Find(date);
                if (existing != null)
                {
                    db.TimeOffLogs.Remove(existing);
                }
                else
                {
                    db.TimeOffLogs.Add(new TimeOffLog
                    {
                        Date = date,
                        Type = "Vacation",
                        Hours = item.Hours
                    });
                }
                db.SaveChanges();

                RefreshWeekData();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error toggling time off day: {ex.Message}");
            }
        }

        private void SaveTimelineBlockChanges()
        {
            if (SelectedTimelineBlock == null) return;

            string[] parts = SelectedTimelineBlock.TimeLabel.Split(" - ");
            if (parts.Length != 2) return;

            if (!TimeSpan.TryParse(parts[0], out TimeSpan startTime) || !TimeSpan.TryParse(parts[1], out TimeSpan endTime))
                return;

            DateTime blockStart = SelectedDate.Date.Add(startTime);
            DateTime blockEnd = SelectedDate.Date.Add(endTime);

            string newCategory = SelectedBlockCategory;
            string newDescription = SelectedBlockDescription;

            try
            {
                using var db = new DatabaseContext();

                // 1. Delete logs fully within the block
                var fullyWithin = db.AppUsageLogs
                    .Where(l => l.StartTime >= blockStart && l.EndTime <= blockEnd)
                    .ToList();
                db.AppUsageLogs.RemoveRange(fullyWithin);

                // 2. Crop logs overlapping the start
                var startOverlaps = db.AppUsageLogs
                    .Where(l => l.StartTime < blockStart && l.EndTime > blockStart && l.EndTime <= blockEnd)
                    .ToList();
                foreach (var log in startOverlaps)
                {
                    log.EndTime = blockStart;
                }

                // 3. Crop logs overlapping the end
                var endOverlaps = db.AppUsageLogs
                    .Where(l => l.StartTime >= blockStart && l.StartTime < blockEnd && l.EndTime > blockEnd)
                    .ToList();
                foreach (var log in endOverlaps)
                {
                    log.StartTime = blockEnd;
                }

                // 4. Split logs spanning across the block
                var spanningLogs = db.AppUsageLogs
                    .Where(l => l.StartTime < blockStart && l.EndTime > blockEnd)
                    .ToList();
                foreach (var log in spanningLogs)
                {
                    db.AppUsageLogs.Add(new AppUsageLog
                    {
                        ProcessName = log.ProcessName,
                        WindowTitle = log.WindowTitle,
                        StartTime = blockEnd,
                        EndTime = log.EndTime,
                        Category = log.Category
                    });
                    log.EndTime = blockStart;
                }

                // 5. Add manual log entry
                if (newCategory != "Away")
                {
                    db.AppUsageLogs.Add(new AppUsageLog
                    {
                        ProcessName = "manual",
                        WindowTitle = string.IsNullOrWhiteSpace(newDescription) ? "Manual Entry" : newDescription,
                        StartTime = blockStart,
                        EndTime = blockEnd,
                        Category = newCategory
                    });
                }

                db.SaveChanges();

                SelectedTimelineBlock = null;

                RefreshDayData();
                RefreshWeekData();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error editing timeline block: {ex.Message}");
            }
        }

        private void DeleteCategoryRule()
        {
            if (string.IsNullOrWhiteSpace(SelectedProcessName)) return;

            string process = ActivityTracker.CleanProcessName(SelectedProcessName);
            try
            {
                using var db = new DatabaseContext();
                var entry = db.AppCategories.Find(process);
                if (entry != null)
                {
                    db.AppCategories.Remove(entry);
                    db.SaveChanges();
                }

                // Reset in cache
                if (System.Windows.Application.Current is App appInstance)
                {
                    appInstance.UpdateCategoryCache(process, "Work");
                }

                SelectedProcessName = string.Empty;
                _originalProcessName = string.Empty;
                SelectedAppCategory = null;

                RefreshCategories();
                RefreshDayData();
                RefreshWeekData();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting category rule: {ex.Message}");
            }
        }



        private void RefreshCalendarView()
        {
            try
            {
                CalendarDays.Clear();
                using var db = new DatabaseContext();
                var logs = db.AppUsageLogs.ToList();
                var timeOffs = GetCombinedTimeOffLogs(db);
                var holidayLogs = db.HolidayLogs.ToList();

                // Real-date time-off entries represent flex hours spent (not year-1900 unlinked gains)
                // Exclude "Holiday" from spent flex hours dictionary
                var usedTimeOffByDate = timeOffs
                    .Where(t => t.Date.Year != 1900 && t.Type != "Holiday")
                    .ToDictionary(t => t.Date.Date, t => t.Hours);

                DateTime firstOfMonth = new DateTime(CalendarMonth.Year, CalendarMonth.Month, 1);
                DateTime gridStart = OvertimeCalculator.GetStartOfWeek(firstOfMonth);

                for (int w = 0; w < 6; w++)
                {
                    DateTime weekStart = gridStart.AddDays(w * 7);
                    DateTime weekEnd = weekStart.AddDays(7);

                    var logsInWeek = logs.Where(l => l.StartTime >= weekStart && l.EndTime <= weekEnd).ToList();
                    var timeOffsInWeek = timeOffs.Where(t => t.Date >= weekStart && t.Date < weekEnd).ToList();

                    double previousCumulative = 0;
                    for (int i = 0; i < 7; i++)
                    {
                        DateTime day = weekStart.AddDays(i);
                        DateTime limitDate = day.AddDays(1);

                        var logsUpToDay = logsInWeek
                            .Where(l => l.StartTime >= weekStart && l.EndTime <= limitDate)
                            .ToList();

                        var timeOffsUpToDay = timeOffsInWeek
                            .Where(t => t.Date >= weekStart && t.Date < limitDate)
                            .ToList();

                        var calcResult = OvertimeCalculator.Calculate(weekStart, logsUpToDay, timeOffsUpToDay);
                        double cumulative = calcResult.TotalOvertimeTimeOffEarned;
                        double gained = cumulative - previousCumulative;
                        previousCumulative = cumulative;

                        // Look up how many flex hours were *spent* (used as time off) on this specific day
                        double timeOffUsed = usedTimeOffByDate.TryGetValue(day.Date, out double used) ? used : 0.0;

                        // Count sick-day hours on this day (stored as sick_time AppUsageLogs)
                        double sickHours = logs
                            .Where(l => l.ProcessName == "sick_time" && l.StartTime.Date == day.Date)
                            .Sum(l => (l.EndTime - l.StartTime).TotalHours);

                        // Find if this date is a registered holiday or a public holiday
                        var holiday = holidayLogs.FirstOrDefault(h => day.Date >= h.StartDate.Date && day.Date <= h.EndDate.Date);
                        string holText = string.Empty;
                        if (holiday != null)
                        {
                            holText = holiday.IsPublicHoliday ? $"🌴 {holiday.Note}" : "🌴 Holiday";
                        }

                        CalendarDays.Add(new CalendarDayItem
                        {
                            DayNumber      = day.Day,
                            Date           = day,
                            IsCurrentMonth = day.Month == CalendarMonth.Month,
                            GainedHours    = gained,
                            TimeOffUsed    = timeOffUsed,
                            SickHours      = sickHours,
                            HolidayText    = holText
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error refreshing calendar view: {ex.Message}");
            }
        }

        private List<TimeOffLog> GetCombinedTimeOffLogs(DatabaseContext db)
        {
            var list = db.TimeOffLogs.ToList();
            var holidays = db.HolidayLogs.ToList();
            
            foreach (var h in holidays)
            {
                DateTime start = h.StartDate.Date;
                DateTime end = h.EndDate.Date;
                for (DateTime dt = start; dt <= end; dt = dt.AddDays(1))
                {
                    if (dt.DayOfWeek != DayOfWeek.Saturday && dt.DayOfWeek != DayOfWeek.Sunday)
                    {
                        // Check if a manual TimeOffLog doesn't already exist for this date
                        if (!list.Any(t => t.Date.Date == dt))
                        {
                            list.Add(new TimeOffLog
                            {
                                Date = dt,
                                Type = "Holiday",
                                Hours = 8.0
                            });
                        }
                    }
                }
            }
            
            return list;
        }

        private int GetUsedHolidayDaysForYear(int year, List<HolidayLog> holidays)
        {
            // Get all public holiday dates for this year
            HashSet<DateTime> publicHolidays = new HashSet<DateTime>();
            try
            {
                using (var db = new DatabaseContext())
                {
                    var allPublicHolidays = db.HolidayLogs
                        .Where(h => h.IsPublicHoliday)
                        .ToList();

                    publicHolidays = allPublicHolidays
                        .Where(h => h.StartDate.Year == year)
                        .Select(h => h.StartDate.Date)
                        .ToHashSet();
                }
            }
            catch { }

            HashSet<DateTime> usedDays = new HashSet<DateTime>();
            DateTime yearStart = new DateTime(year, 1, 1);
            DateTime yearEnd = new DateTime(year, 12, 31);
            
            foreach (var h in holidays)
            {
                DateTime start = h.StartDate.Date;
                DateTime end = h.EndDate.Date;
                
                DateTime overlapStart = start < yearStart ? yearStart : start;
                DateTime overlapEnd = end > yearEnd ? yearEnd : end;
                
                for (DateTime dt = overlapStart; dt <= overlapEnd; dt = dt.AddDays(1))
                {
                    if (dt.DayOfWeek != DayOfWeek.Saturday && dt.DayOfWeek != DayOfWeek.Sunday)
                    {
                        if (!publicHolidays.Contains(dt))
                        {
                            usedDays.Add(dt);
                        }
                    }
                }
            }
            return usedDays.Count;
        }

        private void RegisterHoliday()
        {
            if (HolidayEndDate < HolidayStartDate)
            {
                System.Windows.MessageBox.Show("End Date cannot be before Start Date.", "Invalid Range", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var db = new DatabaseContext();
                var newLog = new HolidayLog
                {
                    StartDate = HolidayStartDate.Date,
                    EndDate = HolidayEndDate.Date,
                    Note = HolidayNote.Trim()
                };

                db.HolidayLogs.Add(newLog);
                db.SaveChanges();

                HolidayNote = string.Empty;
                HolidayStartDate = DateTime.Today;
                HolidayEndDate = DateTime.Today;

                RefreshHolidayLogsList();
                RefreshData();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error registering holiday: {ex.Message}");
            }
        }

        private void DeleteHoliday(HolidayLog? log)
        {
            if (log == null) return;
            try
            {
                using var db = new DatabaseContext();
                var existing = db.HolidayLogs.Find(log.Id);
                if (existing != null)
                {
                    db.HolidayLogs.Remove(existing);
                    db.SaveChanges();
                }

                RefreshHolidayLogsList();
                RefreshData();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting holiday: {ex.Message}");
            }
        }

        public void RefreshHolidayLogsList()
        {
            try
            {
                using var db = new DatabaseContext();
                var today = DateTime.Today;
                var list = db.HolidayLogs.ToList()
                    .Where(h => !h.IsPublicHoliday || h.EndDate.Date >= today)
                    .OrderBy(h => h.StartDate)
                    .ToList();
                HolidayLogs.Clear();
                foreach (var item in list)
                {
                    HolidayLogs.Add(item);
                }
                OnPropertyChanged(nameof(HolidayBalanceText));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading holidays: {ex.Message}");
            }
        }
    }

    public class DailyEventItem
    {
        public string Title { get; set; } = string.Empty;
        public string TimeLabel { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }
}
