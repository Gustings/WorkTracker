using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using Timer = System.Timers.Timer;

namespace WorkTracker.Services
{
    public class ActivityEventArgs : EventArgs
    {
        public string ProcessName { get; }
        public string WindowTitle { get; }
        public DateTime StartTime { get; }
        public DateTime EndTime { get; }
        public string Category { get; }

        public ActivityEventArgs(string processName, string windowTitle, DateTime startTime, DateTime endTime, string category)
        {
            ProcessName = processName;
            WindowTitle = windowTitle;
            StartTime = startTime;
            EndTime = endTime;
            Category = category;
        }
    }

    public class IdleEventArgs : EventArgs
    {
        public DateTime IdleStartTime { get; }
        public double IdleDurationSeconds { get; }

        public IdleEventArgs(DateTime idleStartTime, double idleDurationSeconds)
        {
            IdleStartTime = idleStartTime;
            IdleDurationSeconds = idleDurationSeconds;
        }
    }

    public class ActivityTracker : IDisposable
    {
        // Win32 API declarations
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        private readonly Timer _timer;
        private readonly Func<string, string> _categoryResolver;
        private readonly int _idleThresholdSeconds;

        private string _lastProcessName = string.Empty;
        private string _lastWindowTitle = string.Empty;
        private DateTime _lastActiveTime;
        private DateTime _idleStartTime;

        private bool _isIdle = false;

        public event EventHandler<ActivityEventArgs>? ActivityLogged;
        public event EventHandler<IdleEventArgs>? UserReturnedFromIdle;

        public ActivityTracker(Func<string, string> categoryResolver, int idleThresholdSeconds = 300)
        {
            _categoryResolver = categoryResolver;
            _idleThresholdSeconds = idleThresholdSeconds;

            _timer = new Timer(1000); // Check every second
            _timer.Elapsed += OnTimerElapsed;
            
            _lastActiveTime = DateTime.Now;
        }

        public void Start()
        {
            _lastActiveTime = DateTime.Now;
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
            FlushCurrentActivity();
        }

        private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                uint idleTimeMs = GetSystemIdleTimeMs();
                double idleTimeSec = idleTimeMs / 1000.0;

                if (idleTimeSec >= _idleThresholdSeconds)
                {
                    if (!_isIdle)
                    {
                        // Transitioning to Idle
                        _isIdle = true;
                        _idleStartTime = DateTime.Now.AddSeconds(-idleTimeSec);
                        
                        // Flush the active app log up to when the user became idle
                        FlushCurrentActivity(_idleStartTime);
                    }
                }
                else
                {
                    if (_isIdle)
                    {
                        // Returning from Idle
                        _isIdle = false;
                        double idleDuration = (DateTime.Now - _idleStartTime).TotalSeconds;
                        
                        // Fire event so UI can display the return-from-idle prompt
                        UserReturnedFromIdle?.Invoke(this, new IdleEventArgs(_idleStartTime, idleDuration));

                        _lastActiveTime = DateTime.Now;
                        _lastProcessName = string.Empty;
                        _lastWindowTitle = string.Empty;
                    }

                    TrackForegroundWindow();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ActivityTracker: {ex.Message}");
            }
        }

        private void TrackForegroundWindow()
        {
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return;

            GetWindowThreadProcessId(hwnd, out uint pid);
            if (pid == 0) return;

            string processName = "Unknown";
            string windowTitle = "";

            try
            {
                using var proc = Process.GetProcessById((int)pid);
                processName = CleanProcessName(proc.ProcessName);
                windowTitle = proc.MainWindowTitle;
            }
            catch
            {
                // Process might have exited between calls
                if (!string.IsNullOrEmpty(_lastProcessName))
                {
                    processName = _lastProcessName;
                    windowTitle = _lastWindowTitle;
                }
            }

            DateTime now = DateTime.Now;

            // If the active app has changed, flush the previous app's duration to DB
            if (processName != _lastProcessName || windowTitle != _lastWindowTitle)
            {
                FlushCurrentActivity(now);

                _lastProcessName = processName;
                _lastWindowTitle = windowTitle;
                _lastActiveTime = now;
            }
        }

        private void FlushCurrentActivity(DateTime? endTime = null)
        {
            if (string.IsNullOrEmpty(_lastProcessName)) return;

            DateTime end = endTime ?? DateTime.Now;
            if (end <= _lastActiveTime) return;

            string category = _categoryResolver(_lastProcessName);
            
            // Only fire if the app category is not set to "Ignore"
            if (category != "Ignore")
            {
                ActivityLogged?.Invoke(this, new ActivityEventArgs(
                    _lastProcessName,
                    _lastWindowTitle,
                    _lastActiveTime,
                    end,
                    category
                ));
            }

            _lastProcessName = string.Empty;
            _lastWindowTitle = string.Empty;
        }

        private uint GetSystemIdleTimeMs()
        {
            var lii = new LASTINPUTINFO();
            lii.cbSize = (uint)Marshal.SizeOf(lii);
            if (GetLastInputInfo(ref lii))
            {
                uint currentTick = (uint)Environment.TickCount;
                return currentTick - lii.dwTime;
            }
            return 0;
        }

        public static string CleanProcessName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            string cleaned = name.ToLower().Trim();
            if (cleaned.EndsWith(".exe"))
            {
                cleaned = cleaned.Substring(0, cleaned.Length - 4);
            }
            if (cleaned.EndsWith(".root"))
            {
                cleaned = cleaned.Substring(0, cleaned.Length - 5);
            }
            return cleaned.Trim();
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
