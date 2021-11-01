using NetworkedPlugins.API.Interfaces;

namespace BetterReports
{
    public class BetterReportsDedicatedConfig : IConfig
    {
        public bool IsEnabled { get; set; } = true;
    }
}
