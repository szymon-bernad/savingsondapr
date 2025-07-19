namespace SavingsPlatform.Common.Config;

public record AccountsApiConfig
{
    public required string AccountsApiServiceName { get; init; }
    
    public required string DebitAccountEndpoint { get; init; } = "/api/accounts/:debit";
    
    public required string CreditAccountEndpoint { get; init; } = "/api/accounts/:credit";

    public required string AccountHoldersEndpoint { get; init; } = "/api/account-holders/{0}";

    public required string AccountsByIdsEndpoint { get; init; } = "/api/platform/accounts/:query-by-ids";

    public required string AddAccountsEndpoint { get; init; } = "/api/account-holders/{0}/accounts";

    public required string CreateCurrentAccountEndpoint { get; init; } = "/api/accounts";
}
