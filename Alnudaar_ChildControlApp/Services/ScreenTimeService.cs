using System.Diagnostics;
using Alnudaar_ChildControlApp.Models;
using System.Threading;
using System.Windows.Forms;
using System.Threading.Tasks;



namespace Alnudaar_ChildControlApp.Services
{
    public class ScreenTimeService
    {
        private readonly DatabaseService _databaseService;
        private readonly ILogger<ScreenTimeService> _logger;

        private Thread? _blockFormThread;
        private BlockForm? _blockForm;

        public ScreenTimeService(DatabaseService databaseService, ILogger<ScreenTimeService> logger)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        public async Task EnforceScreenTimeSchedulesAsync(CancellationToken stoppingToken)
        {
            var schedules = _databaseService.GetScreenTimeSchedules();
            var now = DateTime.Now;
            var currentDay = now.DayOfWeek.ToString();
            var currentTime = now.TimeOfDay;

            var isAllowed = schedules.Any(schedule =>
                schedule.DayOfWeek == currentDay &&
                TimeSpan.Parse(schedule.StartTime) <= currentTime &&
                TimeSpan.Parse(schedule.EndTime) >= currentTime);

            var nextAllowed = GetNextRelevantTime(schedules);
            string nextAllowedTime = nextAllowed.HasValue ? nextAllowed.Value.ToString("f") : "Unknown";

            if (!isAllowed)
            {
                ShowBlockForm(nextAllowedTime);
            }
            else
            {
                CloseBlockForm();
            }
        }

        private DateTime? GetNextRelevantTime(IEnumerable<ScreenTimeSchedule> schedules)
        {
            var now = DateTime.Now;
            var currentDay = now.DayOfWeek.ToString();

            // Find the next relevant time (StartTime or EndTime)
            var nextTimes = schedules
                .Where(schedule => schedule.DayOfWeek == currentDay)
                .SelectMany(schedule => new[]
                {
                    DateTime.Today.Add(TimeSpan.Parse(schedule.StartTime)),
                    DateTime.Today.Add(TimeSpan.Parse(schedule.EndTime))
                })
                .Where(time => time > now) // Only consider future times
                .OrderBy(time => time) // Sort by the nearest time
                .ToList();

            return nextTimes.FirstOrDefault(); // Return the nearest time, or null if none
        }

        private void LockWindowsSession()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "rundll32.exe",
                Arguments = "user32.dll,LockWorkStation",
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }

        private void ShowBlockForm(string nextAllowedTime)
        {
            if (_blockFormThread != null && _blockFormThread.IsAlive)
                return;

            _blockFormThread = new Thread(() =>
            {
                _blockForm = new BlockForm(nextAllowedTime);
                Application.Run(_blockForm);
            });
            _blockFormThread.SetApartmentState(ApartmentState.STA);
            _blockFormThread.IsBackground = true;
            _blockFormThread.Start();
        }

        private void CloseBlockForm()
        {
            if (_blockForm != null && _blockForm.InvokeRequired)
            {
                _blockForm.Invoke(new Action(() => _blockForm.Close()));
            }
            _blockForm = null;
            _blockFormThread = null;
        }
    }
}