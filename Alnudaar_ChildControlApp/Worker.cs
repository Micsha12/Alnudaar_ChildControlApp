using Alnudaar_ChildControlApp.Models;
using Alnudaar_ChildControlApp.Services;
using System.Text.Json;
using System.Text;
using System.Xml;

namespace Alnudaar_ChildControlApp
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly DatabaseService _databaseService;

        private readonly ILogger<ScreenTimeService> _screenTimeLogger;
        private readonly ILogger<BlockRuleService> _blockRuleLogger;
        private AppUsageTracker? _appUsageTracker;
        public Worker(ILogger<Worker> logger, ILogger<ScreenTimeService> screenTimeLogger, ILogger<BlockRuleService> blockRuleLogger, DatabaseService databaseService)
        {
            _logger = logger;
            _screenTimeLogger = screenTimeLogger;
            _blockRuleLogger = blockRuleLogger;
            _databaseService = databaseService;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string deviceName = GetDeviceName();
            if (string.IsNullOrEmpty(deviceName))
            {
                _logger.LogError("Device name is not set. Exiting application.");
                return;
            }

            var screenTimeService = new ScreenTimeService(_databaseService, _screenTimeLogger);
            var blockRuleService = new BlockRuleService(_databaseService, _blockRuleLogger);

            int deviceId = await FetchAndUpdateDeviceData(deviceName, stoppingToken);
            int userId = GetUserIdForDevice(deviceId);
            _appUsageTracker = new AppUsageTracker(_databaseService, userId, deviceId);

            while (!stoppingToken.IsCancellationRequested)
            {
                // Fetch and update device data
                deviceId = await FetchAndUpdateDeviceData(deviceName, stoppingToken);
                if (deviceId > 0)
                {
                    // Fetch and save additional data (ScreenTimeSchedules and BlockRules)
                    await FetchAndSaveAdditionalData(deviceId, stoppingToken);
                }
                else
                {
                    _logger.LogWarning("Failed to retrieve DeviceID for device name: {DeviceName}", deviceName);
                }

                // Enforce screen time schedules
                await screenTimeService.EnforceScreenTimeSchedulesAsync(stoppingToken);

                // Update blocked websites
                blockRuleService.UpdateBlockedWebsites();

                if (DateTime.Now.Hour == 23 && DateTime.Now.Minute == 59)
                {
                    _appUsageTracker?.SaveDailyUsageToDb();

                    // Fetch all reports for today from the database
                    var reports = _databaseService.GetAppUsageReportsForDate(DateTime.Now.Date);

                    if (reports != null && reports.Count > 0)
                    {
                        Console.WriteLine("Before SendAppUsageReportsToServerAsync");
                        await SendAppUsageReportsToServerAsync(reports, "https://192.168.100.15:7200/api/appusagereport", stoppingToken);
                        Console.WriteLine("After SendAppUsageReportsToServerAsync");
                    }
                }

                // Delay before the next iteration
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private string GetDeviceName()
        {
            const string filePath = "device_name.json";
            if (File.Exists(filePath))
            {
                string deviceName = File.ReadAllText(filePath).Trim();
                if (!string.IsNullOrEmpty(deviceName))
                {
                    return deviceName;
                }
            }

            Console.WriteLine("Enter the device name:");
            string inputDeviceName = Console.ReadLine()?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(inputDeviceName))
            {
                throw new InvalidOperationException("Device name cannot be empty.");
            }

            File.WriteAllText(filePath, inputDeviceName);
            return inputDeviceName;
        }

        private async Task<int> FetchAndUpdateDeviceData(string deviceName, CancellationToken stoppingToken)
        {
            try
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true // Ignore SSL errors (testing only)
                };
                using var httpClient = new HttpClient(handler);
                string url = $"https://192.168.100.15:7200/api/devices/{deviceName}";
                HttpResponseMessage response = await httpClient.GetAsync(url, stoppingToken);

                if (response.IsSuccessStatusCode)
                {
                    string jsonData = await response.Content.ReadAsStringAsync(stoppingToken);
                    _logger.LogInformation("Raw JSON Response: {JsonData}", jsonData);

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase // Enable camelCase deserialization
                    };
                    var device = System.Text.Json.JsonSerializer.Deserialize<Device>(jsonData, options);

                    if (device != null && device.DeviceID > 0 && !string.IsNullOrEmpty(device.Name) && device.UserID > 0)
                    {
                        _logger.LogInformation("Deserialized Device: ID={DeviceID}, Name={Name}, UserID={UserID}", device.DeviceID, device.Name, device.UserID);

                        // Save the user if it doesn't exist
                        if (!_databaseService.UserExists(device.UserID))
                        {
                            var user = await FetchUserFromServerAsync(device.UserID, stoppingToken);
                            if (user != null)
                            {
                                _databaseService.SaveUser(user);
                                _logger.LogInformation("User added: UserID={UserID}, UserName={UserName}, Email={Email}", user.UserID, user.UserName, user.Email);
                            }
                            else
                            {
                                _logger.LogWarning("User with UserID={UserID} could not be fetched from server.", device.UserID);
                            }
                        }

                        // Save the device
                        _databaseService.SaveDeviceInfo(device);
                        _databaseService.SaveDevicesInfo(device);
                        _logger.LogInformation("Device added: DeviceID={DeviceID}, Name={Name}", device.DeviceID, device.Name);

                        return device.DeviceID; // Return the DeviceID
                    }
                    else
                    {
                        _logger.LogWarning("Invalid device data. DeviceID={DeviceID}, Name={Name}, UserID={UserID}", device?.DeviceID, device?.Name, device?.UserID);
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to fetch device data. Status Code: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching device data.");
            }

            return 0; // Return 0 if the DeviceID could not be retrieved
        }

        private async Task<User?> FetchUserFromServerAsync(int userId, CancellationToken stoppingToken)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true // Ignore SSL errors (testing only)
            };
            using var httpClient = new HttpClient(handler);

            string url = $"https://192.168.100.15:7200/api/users/{userId}";
            HttpResponseMessage response = await httpClient.GetAsync(url, stoppingToken);

            if (response.IsSuccessStatusCode)
            {
                string jsonData = await response.Content.ReadAsStringAsync(stoppingToken);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var user = System.Text.Json.JsonSerializer.Deserialize<User>(jsonData, options);
                return user;
            }
            else
            {
                _logger.LogWarning("Failed to fetch user info for UserID={UserID}. Status Code: {StatusCode}", userId, response.StatusCode);
                return null;
            }
        }
        private async Task FetchAndSaveAdditionalData(int deviceId, CancellationToken stoppingToken)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true // Ignore SSL errors (testing only)
            };
            using var httpClient = new HttpClient(handler);

            // Fetch Screen Time Schedule data
            string screenTimeScheduleUrl = $"https://192.168.100.15:7200/api/ScreenTimeSchedule/devices/{deviceId}";
            HttpResponseMessage screenTimeScheduleResponse = await httpClient.GetAsync(screenTimeScheduleUrl, stoppingToken);
            if (screenTimeScheduleResponse.IsSuccessStatusCode)
            {
                string screenTimeScheduleJson = await screenTimeScheduleResponse.Content.ReadAsStringAsync(stoppingToken);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase // Enable camelCase deserialization
                };

                var screenTimeScheduleData = System.Text.Json.JsonSerializer.Deserialize<List<ScreenTimeSchedule>>(screenTimeScheduleJson, options);
                if (screenTimeScheduleData != null)
                {
                    foreach (var schedule in screenTimeScheduleData)
                    {
                        if (schedule.DeviceID == null || string.IsNullOrWhiteSpace(schedule.DayOfWeek) ||
                            string.IsNullOrWhiteSpace(schedule.StartTime) || string.IsNullOrWhiteSpace(schedule.EndTime))
                        {
                            _logger.LogWarning("Skipping invalid ScreenTimeSchedule: DeviceID={DeviceID}, UserID={UserID}, DayOfWeek={DayOfWeek}, StartTime={StartTime}, EndTime={EndTime}",
                                schedule.DeviceID, schedule.UserID, schedule.DayOfWeek, schedule.StartTime, schedule.EndTime);
                            continue;
                        }

                        _logger.LogInformation("Processing ScreenTimeSchedule: DeviceID={DeviceID}, UserID={UserID}, DayOfWeek={DayOfWeek}, StartTime={StartTime}, EndTime={EndTime}",
                            schedule.DeviceID, schedule.UserID, schedule.DayOfWeek, schedule.StartTime, schedule.EndTime);
                    }

                    _databaseService.SaveScreenTimeSchedules(screenTimeScheduleData);
                }
                else
                {
                    _logger.LogWarning("Failed to deserialize Screen Time Schedule data.");
                }
            }
            else
            {
                _logger.LogWarning("Failed to fetch Screen Time Schedule data. Status Code: {StatusCode}", screenTimeScheduleResponse.StatusCode);
            }

            // Fetch BlockRules data
            string blockRulesUrl = $"https://192.168.100.15:7200/api/BlockRules/devices/{deviceId}";
            HttpResponseMessage blockRulesResponse = await httpClient.GetAsync(blockRulesUrl, stoppingToken);
            if (blockRulesResponse.IsSuccessStatusCode)
            {
                string blockRulesJson = await blockRulesResponse.Content.ReadAsStringAsync(stoppingToken);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase // Enable camelCase deserialization
                };

                var blockRulesData = System.Text.Json.JsonSerializer.Deserialize<List<BlockRule>>(blockRulesJson, options);
                if (blockRulesData != null)
                {
                    _logger.LogInformation("Deserialized BlockRules Data: Count={Count}", blockRulesData.Count);
                    _databaseService.SaveBlockRules(blockRulesData);
                }
                else
                {
                    _logger.LogWarning("Failed to deserialize BlockRules data.");
                }
            }
            else
            {
                _logger.LogWarning("Failed to fetch BlockRules data. Status Code: {StatusCode}", blockRulesResponse.StatusCode);
            }
        }

        public async Task<bool> SendAppUsageReportsToServerAsync(IEnumerable<AppUsageReport> reports, string apiUrl, CancellationToken stoppingToken)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true // Ignore SSL errors (testing only)
            };
            using var httpClient = new HttpClient(handler);

            // Prepare payload: convert UsageDuration to minutes (int)
            var payload = reports.Select(r => new
            {
                r.UserID,
                r.DeviceID,
                r.Timestamp,
                r.AppName,
                UsageDuration = r.UsageDuration.ToString() // e.g., "00:15:00"
            }).ToList();

            var json = JsonSerializer.Serialize(payload);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(apiUrl, content, stoppingToken);
            _logger.LogInformation("AppUsageReport POST status: {StatusCode}, content: {Content}", response.StatusCode, await response.Content.ReadAsStringAsync());
            return response.IsSuccessStatusCode;
        }

        private int GetUserIdForDevice(int deviceId)
        {
            // Example: fetch from your local database
            var device = _databaseService.GetDeviceById(deviceId);
            return device?.UserID ?? 0;
        }
        
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _appUsageTracker?.SaveDailyUsageToDb();
            var reports = _databaseService.GetAppUsageReportsForDate(DateTime.Now.Date);
            if (reports != null && reports.Count > 0)
            {
                Console.WriteLine("Before SendAppUsageReportsToServerAsync2");
                await SendAppUsageReportsToServerAsync(reports, "https://192.168.100.15:7200/api/appusagereport", cancellationToken);
            }
            await base.StopAsync(cancellationToken);
        }
    }
}