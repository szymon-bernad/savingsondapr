using Dapr.Client;
using Microsoft.Extensions.Options;
using SavingsPlatform.Common.Config;
using SavingsPlatform.Contracts.Accounts.Models;
using SavingsPlatform.Contracts.Accounts.Requests;
using System.Collections;

namespace Dashboard.Api.ApiClients;

public class ExchangeApiClient(IOptions<ExchangeApiConfig> config,
                               DaprClient daprClient)
    : IExchangeApiClient
{
    private readonly ExchangeApiConfig _config = config.Value
            ?? throw new ArgumentNullException(nameof(config));
    private readonly DaprClient _daprClient = daprClient;
    private readonly HttpClient _httpClient = daprClient.CreateInvokableHttpClient();

    public async Task<CurrencyExchangeResponse> GetExchangeRateAsync(CurrencyExchangeQuery query)
    {
        var response = await _httpClient.PostAsJsonAsync<CurrencyExchangeQuery>($"http://{_config.ApiServiceName}/{_config.RateQueryEndpoint}", query);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CurrencyExchangeResponse>()
            ?? throw new InvalidOperationException("Failed to deserialize exchange rate response.");
    }

    public Task ScheduleExchangeOrderAsync(CurrencyExchangeOrder order)
        => _httpClient.PostAsJsonAsync<CurrencyExchangeOrder>($"http://{_config.ApiServiceName}/{_config.OrderEndpoint}", order);

}
