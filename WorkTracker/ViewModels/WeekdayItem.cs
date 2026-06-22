using System;

namespace WorkTracker.ViewModels
{
    public class WeekdayItem : ViewModelBase
    {
        private string _dayName = string.Empty;
        private string _dateText = string.Empty;
        private string _dateString = string.Empty;
        private bool _isTimeOff;
        private string _buttonText = "Register Time Off";
        private double _hours = 8.0;

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

        public string DateString
        {
            get => _dateString;
            set
            {
                if (SetField(ref _dateString, value))
                {
                    if (DateTime.TryParse(_dateString, out DateTime dt))
                    {
                        Date = dt;
                    }
                }
            }
        }

        public DateTime Date { get; private set; }

        public bool IsTimeOff
        {
            get => _isTimeOff;
            set => SetField(ref _isTimeOff, value);
        }

        public string ButtonText
        {
            get => _buttonText;
            set => SetField(ref _buttonText, value);
        }

        private string _hoursText = "8.0";

        public string HoursText
        {
            get => _hoursText;
            set
            {
                if (SetField(ref _hoursText, value))
                {
                    string normalized = value.Replace(',', '.');
                    if (double.TryParse(normalized, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double h))
                    {
                        if (Math.Abs(_hours - h) > 0.0001)
                        {
                            _hours = h;
                            OnPropertyChanged(nameof(Hours));
                        }
                    }
                }
            }
        }

        public double Hours
        {
            get => _hours;
            set
            {
                if (SetField(ref _hours, value))
                {
                    _hoursText = value.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
                    OnPropertyChanged(nameof(HoursText));
                }
            }
        }
    }
}
