using System.IO;

namespace Alnudaar_ChildControlApp.Services
{
    public class BlockRuleService
    {
        private const string HostsFilePath = @"C:\Windows\System32\drivers\etc\hosts";
        private readonly DatabaseService _databaseService;
        private readonly ILogger<BlockRuleService> _logger;

        public BlockRuleService(DatabaseService databaseService, ILogger<BlockRuleService> logger)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        public void UpdateBlockedWebsites()
        {
            var blockRules = _databaseService.GetBlockRules();
            var blockedWebsites = blockRules.Where(rule => rule.Type == "website").Select(rule => rule.Value).ToList();

            try
            {
                var hostsFileContent = File.ReadAllLines(HostsFilePath).ToList();

                // Remove previously blocked websites
                hostsFileContent.RemoveAll(line => line.Contains("127.0.0.1"));

                // Add new blocked websites
                foreach (var website in blockedWebsites)
                {
                    hostsFileContent.Add($"127.0.0.1 {website}");
                }

                File.WriteAllLines(HostsFilePath, hostsFileContent);
                _logger.LogInformation("Blocked websites updated in hosts file.");
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Failed to update hosts file. Run the application as administrator.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the hosts file.");
            }
        }
    }
}