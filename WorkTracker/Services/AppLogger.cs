using System;
using System.Collections.Concurrent;

namespace WorkTracker
{
    public static class AppLogger
    {
        private static readonly ConcurrentQueue<string> _logs = new();
        public static event Action? LogAdded;

        public static void Log(string message)
        {
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            System.Diagnostics.Debug.WriteLine(logEntry);
            _logs.Enqueue(logEntry);
            
            // Keep last 500 entries
            while (_logs.Count > 500)
            {
                _logs.TryDequeue(out _);
            }

            LogAdded?.Invoke();
        }

        public static string GetLogs()
        {
            return string.Join(Environment.NewLine, _logs);
        }
    }
}
