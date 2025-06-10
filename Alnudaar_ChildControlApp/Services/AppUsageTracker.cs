using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Timers;

namespace Alnudaar_ChildControlApp.Services
{
    public class AppUsageTracker
    {
        private readonly DatabaseService _databaseService;
        private readonly int _userId;
        private readonly int _deviceId;
        private readonly Dictionary<string, int> _dailyUsage = new();
        private string? _currentApp;
        private DateTime _lastSwitch;
        private System.Timers.Timer _timer;

        public AppUsageTracker(DatabaseService databaseService, int userId, int deviceId)
        {
            _databaseService = databaseService;
            _userId = userId;
            _deviceId = deviceId;
            _timer = new System.Timers.Timer(5000); // 5 seconds
            _timer.Elapsed += Timer_Elapsed;
            _timer.Start();
            _lastSwitch = DateTime.Now;
            _currentApp = GetActiveProcessName();
        }

        private static readonly HashSet<string> IgnoredApps = new(StringComparer.OrdinalIgnoreCase)
        {
            "explorer", "system", "Idle", "SearchUI", "ShellExperienceHost", "StartMenuExperienceHost", "VsDebugConsole"
            // Add more as needed
        };

        private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            string activeApp = GetActiveProcessName();
            if (IgnoredApps.Contains(activeApp))
                return;

            if (activeApp != _currentApp)
            {
                int seconds = (int)(DateTime.Now - _lastSwitch).TotalSeconds;
                if (!string.IsNullOrEmpty(_currentApp) && !IgnoredApps.Contains(_currentApp))
                {
                    if (_dailyUsage.ContainsKey(_currentApp))
                        _dailyUsage[_currentApp] += seconds;
                    else
                        _dailyUsage[_currentApp] = seconds;
                }
                _currentApp = activeApp;
                _lastSwitch = DateTime.Now;
            }
        }

        public void SaveDailyUsageToDb()
        {
            var saveTime = DateTime.Now;
            foreach (var kvp in _dailyUsage)
            {
                if (IgnoredApps.Contains(kvp.Key))
                continue;

                _databaseService.UpsertAppUsageReport(_userId, _deviceId, saveTime, kvp.Key, kvp.Value);
            }
            _dailyUsage.Clear();
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private string GetActiveProcessName()
        {
            IntPtr hwnd = GetForegroundWindow();
            GetWindowThreadProcessId(hwnd, out uint pid);
            try
            {
                var proc = Process.GetProcessById((int)pid);
                return proc.ProcessName;
            }
            catch
            {
                return "Unknown";
            }
        }
        
    }
}