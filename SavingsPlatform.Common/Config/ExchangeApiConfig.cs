namespace SavingsPlatform.Common.Config;

public record ExchangeApiConfig
{
    public required string ApiServiceName { get; init; }

    public required string OrderEndpoint { get; init; } = "/v1/currency-exchange-order";
}
