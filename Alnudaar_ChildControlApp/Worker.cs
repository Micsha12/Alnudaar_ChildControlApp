using Alnudaar_ChildControlApp.Models;
using System.Text.Json;

namespace Alnudaar_ChildControlApp
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly DatabaseService _databaseService;

        public Worker(ILogger<Worker> logger, DatabaseService databaseService)
        {
            _logger = logger;
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

            _logger.LogInformation("Worker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                int deviceId = await FetchAndUpdateDeviceData(deviceName, stoppingToken);
                if (deviceId > 0)
                {
                    await FetchAndSaveAdditionalData(deviceId, stoppingToken);
                }
                else
                {
                    _logger.LogWarning("Failed to retrieve DeviceID for device name: {DeviceName}", deviceName);
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        private string GetDeviceName()
        {
            const string filePath = "device_name.json";
            if (File.Exists(filePath))
            {
                return File.ReadAllText(filePath);
            }

            Console.WriteLine("Enter the device name:");
            string deviceName = Console.ReadLine() ?? string.Empty;
            File.WriteAllText(filePath, deviceName);
            return deviceName;
        }

        private async Task<int> FetchAndUpdateDeviceData(string deviceName, CancellationToken stoppingToken)
        {
            try
            {
                using var httpClient = new HttpClient();
                string url = $"https://localhost:7200/api/devices/{deviceName}";
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
                            var user = new User
                            {
                                UserID = device.UserID,
                                UserName = "DefaultUser", // Replace with actual user data if available
                                Email = "default@example.com" // Replace with actual email if available
                            };
                            _databaseService.SaveUser(user);
                            _logger.LogInformation("User added: UserID={UserID}", user.UserID);
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
        private async Task FetchAndSaveAdditionalData(int deviceId, CancellationToken stoppingToken)
        {
            using var httpClient = new HttpClient();

            // Fetch Screen Time Schedule data
            string screenTimeScheduleUrl = $"https://localhost:7200/api/ScreenTimeSchedule/devices/{deviceId}";
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
            string blockRulesUrl = $"https://localhost:7200/api/BlockRules/devices/{deviceId}";
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
    }
}