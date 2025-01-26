namespace SavingsPlatform.Common.Config;

public record AccountsApiConfig
{
    public required string AccountsApiServiceName { get; init; }
    
    public required string DebitAccountEndpoint { get; init; } = "/api/accounts/:debit";
    
    public required string CreditAccountEndpoint { get; init; } = "/api/accounts/:credit";

    public required string AccountHoldersEndpoint { get; init; } = "/api/account-holders/{0}";

    public required string AccountsByIdsEndpoint { get; init; } = "/api/accounts/:query-by-ids";
}
