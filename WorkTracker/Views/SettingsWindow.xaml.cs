using System;
using System.Reflection;
using System.Windows;
using WorkTracker.Services;
using WorkTracker.ViewModels;

namespace WorkTracker.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            // Initial load of logs
            TxtLogs.Text = AppLogger.GetLogs();
            TxtLogs.ScrollToEnd();

            // Display about version
            TxtAboutVersion.Text = $"v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.4.2"}";

            // Subscribe to real-time logs
            AppLogger.LogAdded += OnLogAdded;

            AppLogger.Log("Settings Window opened.");
        }

        private void OnLogAdded()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                TxtLogs.Text = AppLogger.GetLogs();
                TxtLogs.ScrollToEnd();
            }));
        }

        protected override void OnClosed(EventArgs e)
        {
            AppLogger.LogAdded -= OnLogAdded;
            base.OnClosed(e);
        }

        private void MinimizeBtn_Click(object sender, RoutedEventArgs e) =>
            SystemCommands.MinimizeWindow(this);

        private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();

        private void BtnSimIdle_Click(object sender, RoutedEventArgs e)
        {
            var app = System.Windows.Application.Current as App;
            if (app != null)
            {
                AppLogger.Log("User simulated: returning from 15 minutes of inactivity.");
                app.SimulateIdlePopup(900, isLockPrompt: false);
            }
        }

        private void BtnSimUnlock_Click(object sender, RoutedEventArgs e)
        {
            var app = System.Windows.Application.Current as App;
            if (app != null)
            {
                AppLogger.Log("User simulated: returning from 20 minutes lock screen duration.");
                app.SimulateIdlePopup(1200, isLockPrompt: true);
            }
        }

        private void BtnSyncCalendar_Click(object sender, RoutedEventArgs e)
        {
            var app = System.Windows.Application.Current as App;
            if (app != null)
            {
                AppLogger.Log("User triggered: manual calendar sync.");
                app.RefreshIcsUrl();
            }
        }

        private void BtnClearLogs_Click(object sender, RoutedEventArgs e)
        {
            TxtLogs.Text = string.Empty;
            AppLogger.Log("Logs display cleared by user.");
        }

        private void LogUpdate(string message)
        {
            Dispatcher.Invoke(() =>
            {
                TxtUpdateLogs.AppendText($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
                TxtUpdateLogs.ScrollToEnd();
            });
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true
                });
                e.Handled = true;
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error opening hyperlink: {ex.Message}");
            }
        }

        private async void BtnCheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            BtnCheckUpdate.IsEnabled = false;
            BtnCheckUpdate.Content = "Checking...";
            TxtUpdateLogs.Clear();
            LogUpdate("Checking for updates from GitHub...");

            try
            {
                var updateService = new UpdateService();
                var updateInfo = await updateService.CheckForUpdatesAsync(LogUpdate);

                if (updateInfo.HasUpdate)
                {
                    LogUpdate($"New update available: {updateInfo.LatestVersion}");
                    var result = System.Windows.MessageBox.Show(
                        this,
                        $"A new update (v{updateInfo.LatestVersion}) is available.\n\nRelease Notes:\n{updateInfo.ReleaseNotes}\n\nWould you like to download and install it silently now?",
                        "Update Available",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question
                    );

                    if (result == MessageBoxResult.Yes)
                    {
                        LogUpdate($"Downloading update from: {updateInfo.DownloadUrl}");
                        BtnCheckUpdate.Content = "Downloading 0%...";

                        await updateService.DownloadAndInstallUpdateAsync(updateInfo.DownloadUrl, progress =>
                        {
                            Dispatcher.Invoke(() =>
                            {
                                BtnCheckUpdate.Content = $"Downloading {(int)(progress * 100)}%...";
                            });
                        }, LogUpdate);
                        return; // App will shutdown inside DownloadAndInstallUpdateAsync
                    }
                    else
                    {
                        LogUpdate("Update download deferred by user.");
                    }
                }
                else
                {
                    Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);
                    LogUpdate($"Application is up to date (current: v{currentVersion.ToString(3)})");
                    System.Windows.MessageBox.Show(
                        this,
                        $"Your application is up to date.\n\nCurrent Version: v{currentVersion.ToString(3)}",
                        "No Updates Found",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
            }
            catch (Exception ex)
            {
                LogUpdate($"Error checking/downloading update: {ex.Message}");
                System.Windows.MessageBox.Show(
                    this,
                    $"Failed to check or install update: {ex.Message}",
                    "Update Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            finally
            {
                BtnCheckUpdate.IsEnabled = true;
                BtnCheckUpdate.Content = "Check for Updates";
            }
        }
    }
}
