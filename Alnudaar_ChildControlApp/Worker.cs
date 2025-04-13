using Alnudaar_ChildControlApp.Models;

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
                    var device = System.Text.Json.JsonSerializer.Deserialize<Device>(jsonData);

                    // Save data to the local database
                    if (device != null)
                    {
                        _databaseService.SaveDeviceInfo(device);

                        if (device.ScreenTimeSchedules != null)
                        {
                            _databaseService.SaveScreenTimeSchedules(device.ScreenTimeSchedules);
                        }

                        _logger.LogInformation("Device data updated successfully.");
                    }
                    else
                    {
                        _logger.LogWarning("Deserialized device is null.");
                    }

                    _logger.LogInformation("Device data updated successfully.");
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
    }
}