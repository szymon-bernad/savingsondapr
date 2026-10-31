using Dapr.Client;
using Microsoft.Extensions.Options;
using SavingsPlatform.Common.Config;
using SavingsPlatform.Contracts.Accounts.Requests;
using SavingsPlatform.Contracts.CurrencyExchange.Response;

namespace Dashboard.Api.ApiClients;

public class EventStoreApiClient(
    IOptions<EventStoreApiConfig> config,
    DaprClient daprClient,
    HttpClient httpClient) : IEventStoreApiClient
{
    private readonly EventStoreApiConfig _config = config.Value
            ?? throw new ArgumentNullException(nameof(config));


    public async Task InitiateCurrencyExchangeSummaryAsync(CurrencyExchangeSummaryRequest request)
    {
        var req = daprClient.CreateInvokeMethodRequest<CurrencyExchangeSummaryRequest>(
                HttpMethod.Post,
                _config.EventStoreApiServiceName,
                _config.InitCurrencyExchangeSummaryEndpoint,
                [],
                request);

        var response = await httpClient.SendAsync(req);
        response.EnsureSuccessStatusCode();
    }


    public Task<CurrencyExchangeSummaryResponse> FetchCurrencyExchangeSummaryAsync(CurrencyExchangeSummaryRequest request)
    {
        throw new NotImplementedException();
    }
}
