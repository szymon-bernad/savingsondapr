namespace SavingsPlatform.Common.Config;

public record ExchangeApiConfig
{
    public required string ApiServiceName { get; init; }

    public required string OrderEndpoint { get; init; } = "/v1/currency-exchange-order";

    public required string RateQueryEndpoint { get; init; } = "/v1/currency-exchange-query";
}
