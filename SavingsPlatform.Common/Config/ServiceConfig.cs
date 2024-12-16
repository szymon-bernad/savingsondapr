namespace SavingsPlatform.Common.Config;

public class ServiceConfig
{
    public string Version { get; set; } = "std";

    public bool UseAzureMonitor { get; set; } = false;
}
