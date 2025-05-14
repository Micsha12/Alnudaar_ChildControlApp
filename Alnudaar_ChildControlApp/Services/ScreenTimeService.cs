using System.Diagnostics;
using Alnudaar_ChildControlApp.Models;

namespace Alnudaar_ChildControlApp.Services
{
    public class ScreenTimeService
    {
        private readonly DatabaseService _databaseService;
        private readonly ILogger<ScreenTimeService> _logger;

        public ScreenTimeService(DatabaseService databaseService, ILogger<ScreenTimeService> logger)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        public async Task EnforceScreenTimeSchedulesAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var schedules = _databaseService.GetScreenTimeSchedules();
                var nextTime = GetNextRelevantTime(schedules);

                if (nextTime.HasValue)
                {
                    var delay = nextTime.Value - DateTime.Now;
                    _logger.LogInformation("Next screen time check scheduled at: {NextTime}", nextTime.Value);

                    // Wait until the next relevant time
                    await Task.Delay(delay, stoppingToken);
                }
                else
                {
                    _logger.LogInformation("No relevant screen time schedules found for today.");
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // Check again in an hour
                }

                // Check if the current time is within any allowed schedule
                var now = DateTime.Now;
                var currentDay = now.DayOfWeek.ToString();
                var currentTime = now.TimeOfDay;

                var isAllowed = schedules.Any(schedule =>
                    schedule.DayOfWeek == currentDay &&
                    TimeSpan.Parse(schedule.StartTime) <= currentTime &&
                    TimeSpan.Parse(schedule.EndTime) >= currentTime);

                if (!isAllowed)
                {
                    _logger.LogWarning("Screen time exceeded. Locking the session.");
                    LockWindowsSession();
                }
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
    }
}