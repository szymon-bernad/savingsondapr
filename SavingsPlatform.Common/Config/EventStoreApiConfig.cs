namespace SavingsPlatform.Common.Config;

public record EventStoreApiConfig
{
    public required string EventStoreApiServiceName { get; init; }
    public required string BalancesEndpoint { get; init; } = "v1/events/balances-summary/{0}";

    public required string FromQueryParameter { get; init; } = "fromDate";

    public required string ToQueryParameter { get; init; } = "toDate";

    public required string InitCurrencyExchangeSummaryEndpoint { get; init; } = "v1/currency-exchange-summary/:init";

}
