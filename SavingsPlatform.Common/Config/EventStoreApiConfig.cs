namespace SavingsPlatform.Common.Config;

public record EventStoreApiConfig
{
    public required string EventStoreApiServiceName { get; init; }
    public required string TransactionsEndpoint { get; init; } = "v1/events/transactions/{0}";

    public required string FromQueryParameter { get; init; } = "fromDate";

    public required string ToQueryParameter { get; init; } = "toDate";

}
