using System;
using System.Windows;

namespace WorkTracker.Views
{
    public partial class IdlePromptWindow : Window
    {
        public string ResultCategory { get; private set; } = "Meeting";
        public string ResultDescription { get; private set; } = string.Empty;

        public IdlePromptWindow(DateTime idleStart, double durationSeconds, bool isLockPrompt = false)
        {
            InitializeComponent();

            int minutes = (int)Math.Round(durationSeconds / 60.0);
            
            if (isLockPrompt)
            {
                TxtHeader.Text = "PC Locked Summary";
                PanelInactive.Visibility = Visibility.Collapsed;
                PanelLocked.Visibility = Visibility.Visible;
                
                TxtDurationLock.Text = minutes.ToString();
                TxtStartLock.Text = idleStart.ToString("HH:mm");
                TxtEndLock.Text = idleStart.AddSeconds(durationSeconds).ToString("HH:mm");
            }
            else
            {
                TxtHeader.Text = "Welcome Back!";
                PanelInactive.Visibility = Visibility.Visible;
                PanelLocked.Visibility = Visibility.Collapsed;

                TxtDuration.Text = minutes.ToString();
                TxtStart.Text = idleStart.ToString("HH:mm");
                TxtEnd.Text = idleStart.AddSeconds(durationSeconds).ToString("HH:mm");
            }
            
            RadBreak.Checked += (s, e) => TxtDescription.Text = string.Empty;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (RadWork.IsChecked == true)
            {
                ResultCategory = "Offline Work";
                ResultDescription = string.IsNullOrWhiteSpace(TxtDescription.Text) ? "Offline Work" : TxtDescription.Text.Trim();
            }
            else if (RadMeeting.IsChecked == true)
            {
                ResultCategory = "Meeting";
                ResultDescription = string.IsNullOrWhiteSpace(TxtDescription.Text) ? "Meeting" : TxtDescription.Text.Trim();
            }
            else
            {
                ResultCategory = "Break";
                ResultDescription = "Break";
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            ResultCategory = "Break";
            ResultDescription = "Break";
            DialogResult = false;
            Close();
        }
    }
}
