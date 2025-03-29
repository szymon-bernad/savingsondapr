using Dapr.Client;
using Microsoft.Extensions.Options;
using SavingsPlatform.Common.Config;
using SavingsPlatform.Contracts.Accounts.Requests;
using SavingsPlatform.Contracts.CurrencyExchange.Response;

namespace Dashboard.Api.ApiClients;

public class EventStoreApiClient(
    IOptions<EventStoreApiConfig> config,
    DaprClient _daprClient,
    ILogger<EventStoreApiClient> _logger) : IEventStoreApiClient
{
    private readonly EventStoreApiConfig _config = config.Value
            ?? throw new ArgumentNullException(nameof(config));


    public Task InitiateCurrencyExchangeSummaryAsync(CurrencyExchangeSummaryRequest request)
        => _daprClient.InvokeMethodAsync(
                HttpMethod.Post,
                _config.EventStoreApiServiceName,
                _config.InitCurrencyExchangeSummaryEndpoint,
                request);


    public Task<CurrencyExchangeSummaryResponse> FetchCurrencyExchangeSummaryAsync(CurrencyExchangeSummaryRequest request)
    {
        throw new NotImplementedException();
    }
}
