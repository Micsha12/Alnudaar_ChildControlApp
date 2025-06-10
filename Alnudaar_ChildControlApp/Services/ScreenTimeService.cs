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
            var nextTimes = new List<DateTime>();

            foreach (var schedule in schedules)
            {
                // Skip if StartTime or EndTime is null/empty/whitespace
                if (string.IsNullOrWhiteSpace(schedule.StartTime) || string.IsNullOrWhiteSpace(schedule.EndTime))
                    continue;

                for (int i = 0; i < 7; i++)
                {
                    var targetDay = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), schedule.DayOfWeek);
                    var date = now.Date.AddDays((7 + targetDay - now.DayOfWeek + i) % 7);

                    // TryParse to avoid crashing on bad format
                    if (TimeSpan.TryParse(schedule.StartTime, out var startTs))
                    {
                        var startDateTime = date.Add(startTs);
                        if (startDateTime > now)
                            nextTimes.Add(startDateTime);
                    }
                    if (TimeSpan.TryParse(schedule.EndTime, out var endTs))
                    {
                        var endDateTime = date.Add(endTs);
                        if (endDateTime > now)
                            nextTimes.Add(endDateTime);
                    }

                    if (date > now.Date)
                        break;
                }
            }

            return nextTimes.Count > 0 ? nextTimes.Min() : (DateTime?)null;
        }

        private void LogoffWindowsSession()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "shutdown",
                Arguments = "/l", // log off
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

                var timer = new System.Windows.Forms.Timer();
                timer.Interval = 5000;
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    LogoffWindowsSession();
                    CloseBlockForm();
                };
                timer.Start();

                Application.Run(_blockForm);
                CloseBlockForm();
            });
            _blockFormThread.SetApartmentState(ApartmentState.STA);
            _blockFormThread.IsBackground = true;
            _blockFormThread.Start();
        }

        private void CloseBlockForm()
        {
            if (_blockForm != null)
            {
                if (_blockForm.InvokeRequired)
                {
                    _blockForm.Invoke(new Action(() => _blockForm.Close()));
                }
                else
                {
                    _blockForm.Close();
                }
                _blockForm = null;
            }
            _blockFormThread = null;
        }
    }
}