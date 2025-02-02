using Dapr.Client;
using Microsoft.Extensions.Options;
using SavingsPlatform.Common.Config;
using SavingsPlatform.Contracts.Accounts.Requests;

namespace Dashboard.Api.ApiClients;

public class ExchangeApiClient(IOptions<ExchangeApiConfig> config,
                               DaprClient daprClient) 
    : IExchangeApiClient
{
    private readonly ExchangeApiConfig _config = config.Value
            ?? throw new ArgumentNullException(nameof(config));
    private readonly DaprClient _daprClient = daprClient;

    public Task ScheduleExchangeOrderAsync(CurrencyExchangeOrder order)
    =>  _daprClient.InvokeMethodAsync(
                HttpMethod.Post,
                _config.ApiServiceName,
                _config.OrderEndpoint,
                order);
}
