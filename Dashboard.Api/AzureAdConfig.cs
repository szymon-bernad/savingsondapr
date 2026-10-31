namespace Dashboard.Api;

internal record AzureAdConfig
{
    public required string Instance { get; init; }
    public required string TenantId { get; init; }
    public required string ClientId { get; init; }
}
