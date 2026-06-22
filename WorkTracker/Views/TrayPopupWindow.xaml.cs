using System;
using System.Windows;
using System.Windows.Input;
using WorkTracker.ViewModels;

namespace WorkTracker.Views
{
    public partial class TrayPopupWindow : Window
    {
        private bool _isClosing = false;

        public TrayPopupWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            PositionWindow();
        }

        private void PositionWindow()
        {
            var desktopWorkingArea = SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width - 10;
            this.Top = desktopWorkingArea.Bottom - this.Height - 10;
        }

        private void CloseWindow()
        {
            if (_isClosing) return;
            _isClosing = true;
            try
            {
                this.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error closing tray popup: {ex.Message}");
            }
        }

        private void Window_Deactivated(object? sender, EventArgs e)
        {
            if (System.Windows.Application.Current is App app)
            {
                app.RegisterDeactivation();
            }
            CloseWindow();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            CloseWindow();
        }

        private void OpenDashboard_Click(object sender, MouseButtonEventArgs e)
        {
            if (System.Windows.Application.Current is App app)
            {
                app.ShowMainWindow();
            }
            CloseWindow();
        }
    }
}
