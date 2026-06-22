using System;
using System.Windows;

namespace WorkTracker.Views
{
    public partial class UnlinkedHoursWindow : Window
    {
        public double Hours { get; set; }

        public UnlinkedHoursWindow(double currentHours)
        {
            InitializeComponent();
            HoursInput.Text = currentHours > 0 ? currentHours.ToString("F2") : "0.00";
            
            // Set focus and select text
            HoursInput.Focus();
            HoursInput.SelectAll();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string text = HoursInput.Text.Replace(',', '.').Trim();
            if (double.TryParse(text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double val))
            {
                Hours = val;
                DialogResult = true;
                Close();
            }
            else
            {
                System.Windows.MessageBox.Show("Please enter a valid number of hours.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
