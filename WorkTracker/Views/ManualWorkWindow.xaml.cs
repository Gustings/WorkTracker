using System;
using System.Windows;

namespace WorkTracker.Views
{
    public partial class ManualWorkWindow : Window
    {
        private DateTime _date;

        public double NormalHours    { get; set; }
        public double Ot16Hours      { get; set; }
        public double Ot21Hours      { get; set; }
        public double TimeOffHours   { get; set; }
        public double SickDayHours   { get; set; }

        public ManualWorkWindow(DateTime date, double normal, double ot16, double ot21, double timeOff, double sickDay = 0)
        {
            InitializeComponent();
            _date = date;
            
            HeaderTitle.Text = $"Add Offline Work - {date:dddd, MMM dd}";
            
            NormalHoursInput.Text    = normal   > 0 ? normal.ToString("F1")   : "0.0";
            Ot16HoursInput.Text      = ot16     > 0 ? ot16.ToString("F1")     : "0.0";
            Ot21HoursInput.Text      = ot21     > 0 ? ot21.ToString("F1")     : "0.0";
            TimeOffHoursInput.Text   = timeOff  > 0 ? timeOff.ToString("F1")  : "0.0";
            SickDayHoursInput.Text   = sickDay  > 0 ? sickDay.ToString("F1")  : "0.0";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (ParseHours(NormalHoursInput.Text,  out double normal) &&
                ParseHours(Ot16HoursInput.Text,    out double ot16)   &&
                ParseHours(Ot21HoursInput.Text,    out double ot21)   &&
                ParseHours(TimeOffHoursInput.Text,  out double timeOff) &&
                ParseHours(SickDayHoursInput.Text,  out double sickDay))
            {
                NormalHours    = normal;
                Ot16Hours      = ot16;
                Ot21Hours      = ot21;
                TimeOffHours   = timeOff;
                SickDayHours   = sickDay;
                DialogResult = true;
                Close();
            }
            else
            {
                System.Windows.MessageBox.Show("Please enter valid decimal numbers for hours (e.g. 1.5 or 2,0).", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool ParseHours(string text, out double hours)
        {
            hours = 0;
            if (string.IsNullOrWhiteSpace(text)) return true;
            string normalized = text.Replace(',', '.').Trim();
            return double.TryParse(normalized, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out hours);
        }
    }
}
