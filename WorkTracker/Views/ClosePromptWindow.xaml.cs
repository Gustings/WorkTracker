using System;
using System.Windows;

namespace WorkTracker.Views
{
    public enum CloseAction
    {
        Cancel,
        MinimizeToTray,
        ExitApplication
    }

    public partial class ClosePromptWindow : Window
    {
        public CloseAction ResultAction { get; private set; } = CloseAction.Cancel;

        public ClosePromptWindow()
        {
            InitializeComponent();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            ResultAction = CloseAction.MinimizeToTray;
            DialogResult = true;
            Close();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            ResultAction = CloseAction.ExitApplication;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            ResultAction = CloseAction.Cancel;
            DialogResult = false;
            Close();
        }
    }
}
