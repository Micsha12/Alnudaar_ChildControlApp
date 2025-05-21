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
            Console.WriteLine("UpdateBlockedWebsites CALLED");
            _logger.LogInformation("UpdateBlockedWebsites called.");
            var blockRules = _databaseService.GetBlockRules();
            var blockedWebsites = blockRules
                .Where(rule => rule.Type == "website" && !string.IsNullOrWhiteSpace(rule.Value))
                .Select(rule => rule.Value.Trim().ToLower())
                .Distinct()
                .ToList();

            try
            {
                var hostsFileContent = File.ReadAllLines(HostsFilePath).ToList();

                // Remove previously blocked websites by this app
                hostsFileContent.RemoveAll(line => line.Contains("# AlnudaarBlock"));

                // Add new blocked websites
                foreach (var website in blockedWebsites)
                {
                    hostsFileContent.Add($"127.0.0.1 {website} # AlnudaarBlock");
                    if (!website.StartsWith("www."))
                    {
                        hostsFileContent.Add($"127.0.0.1 www.{website} # AlnudaarBlock");
                    }
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