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
                await FetchAndUpdateDeviceData(deviceName, stoppingToken);
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

        private async Task FetchAndUpdateDeviceData(string deviceName, CancellationToken stoppingToken)
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
                        _databaseService.SaveDeviceInfo(device);
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
        }

        private async Task FetchAndSaveAdditionalData(int deviceId, CancellationToken stoppingToken)
        {
            using var httpClient = new HttpClient();

            // Fetch Geofencing data
            string geofencingUrl = $"https://localhost:7200/api/devices/{deviceId}/geofencing";
            HttpResponseMessage geofencingResponse = await httpClient.GetAsync(geofencingUrl, stoppingToken);
            if (geofencingResponse.IsSuccessStatusCode)
            {
                string geofencingJson = await geofencingResponse.Content.ReadAsStringAsync(stoppingToken);
                var geofencingData = System.Text.Json.JsonSerializer.Deserialize<List<Geofencing>>(geofencingJson);
                if (geofencingData != null)
                {
                    _databaseService.SaveGeofencingData(geofencingData);
                }
            }

            // Fetch BlockRules data
            string blockRulesUrl = $"https://localhost:7200/api/devices/{deviceId}/blockrules";
            HttpResponseMessage blockRulesResponse = await httpClient.GetAsync(blockRulesUrl, stoppingToken);
            if (blockRulesResponse.IsSuccessStatusCode)
            {
                string blockRulesJson = await blockRulesResponse.Content.ReadAsStringAsync(stoppingToken);
                var blockRulesData = System.Text.Json.JsonSerializer.Deserialize<List<BlockRule>>(blockRulesJson);
                if (blockRulesData != null)
                {
                    _databaseService.SaveBlockRules(blockRulesData);
                }
            }

            // Add similar logic for other data types if needed
        }
    }
}