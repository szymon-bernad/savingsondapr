namespace Dashboard.Api;

internal record AzureAdConfig
{
    public string Instance { get; init; }
    public string TenantId { get; init; }
    public string ClientId { get; init; }
}
