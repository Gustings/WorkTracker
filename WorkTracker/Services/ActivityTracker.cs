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
        public bool WasFullScreen { get; }
        public string LastProcessName { get; }

        public IdleEventArgs(DateTime idleStartTime, double idleDurationSeconds, bool wasFullScreen, string lastProcessName)
        {
            IdleStartTime = idleStartTime;
            IdleDurationSeconds = idleDurationSeconds;
            WasFullScreen = wasFullScreen;
            LastProcessName = lastProcessName;
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
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        private const uint MONITOR_DEFAULTTONEAREST = 2;

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

        private IntPtr _lastHwnd = IntPtr.Zero;
        private uint _cachedPid = 0;
        private string _cachedProcessName = string.Empty;

        private bool _isIdle = false;
        private bool _idleWasFullScreen = false;
        private string _idleLastProcessName = string.Empty;

        public event EventHandler<ActivityEventArgs>? ActivityLogged;
        public event EventHandler<IdleEventArgs>? UserReturnedFromIdle;

        public static bool IsForegroundWindowFullScreen()
        {
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return false;

            if (!GetWindowRect(hwnd, out RECT rect)) return false;

            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (monitor == IntPtr.Zero) return false;

            var monitorInfo = new MONITORINFO();
            monitorInfo.cbSize = Marshal.SizeOf(monitorInfo);
            if (!GetMonitorInfo(monitor, ref monitorInfo)) return false;

            // Check if window matches or exceeds the monitor bounds
            bool matchesWidth = (rect.Left <= monitorInfo.rcMonitor.Left && rect.Right >= monitorInfo.rcMonitor.Right);
            bool matchesHeight = (rect.Top <= monitorInfo.rcMonitor.Top && rect.Bottom >= monitorInfo.rcMonitor.Bottom);

            return matchesWidth && matchesHeight;
        }

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
                        
                        // Capture foreground app metadata right before/at idle transition
                        _idleWasFullScreen = IsForegroundWindowFullScreen();
                        _idleLastProcessName = _lastProcessName;

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
                        UserReturnedFromIdle?.Invoke(this, new IdleEventArgs(_idleStartTime, idleDuration, _idleWasFullScreen, _idleLastProcessName));

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

        private static string GetWindowTitleFromHwnd(IntPtr hwnd)
        {
            StringBuilder sb = new StringBuilder(256);
            if (GetWindowText(hwnd, sb, sb.Capacity) > 0)
            {
                return sb.ToString();
            }
            return string.Empty;
        }

        private void TrackForegroundWindow()
        {
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return;

            GetWindowThreadProcessId(hwnd, out uint pid);
            if (pid == 0) return;

            string processName = "Unknown";
            string windowTitle = "";

            if (hwnd == _lastHwnd && pid == _cachedPid && !string.IsNullOrEmpty(_cachedProcessName))
            {
                processName = _cachedProcessName;
                windowTitle = GetWindowTitleFromHwnd(hwnd);
            }
            else
            {
                try
                {
                    using var proc = Process.GetProcessById((int)pid);
                    processName = CleanProcessName(proc.ProcessName);
                    windowTitle = proc.MainWindowTitle;
                    if (string.IsNullOrWhiteSpace(windowTitle))
                    {
                        windowTitle = GetWindowTitleFromHwnd(hwnd);
                    }

                    _lastHwnd = hwnd;
                    _cachedPid = pid;
                    _cachedProcessName = processName;
                }
                catch
                {
                    if (!string.IsNullOrEmpty(_lastProcessName))
                    {
                        processName = _lastProcessName;
                        windowTitle = _lastWindowTitle;
                    }
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
