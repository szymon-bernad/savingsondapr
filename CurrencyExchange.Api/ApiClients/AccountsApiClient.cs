using Dapr.Client;
using Microsoft.Extensions.Options;
using SavingsPlatform.Common.Config;
using SavingsPlatform.Contracts.Accounts.Requests;

namespace CurrencyExchange.Api.ApiClients;

public class AccountsApiClient(
    IOptions<AccountsApiConfig> cfgOptions,
    DaprClient daprClient,
    ILogger<AccountsApiClient> logger) 
        : IAccountsApiClient
{
    private readonly AccountsApiConfig _config = cfgOptions.Value
            ?? throw new ArgumentNullException(nameof(cfgOptions));
    private readonly DaprClient _daprClient = daprClient;
    private readonly HttpClient _httpClient = daprClient.CreateInvokableHttpClient();
    private readonly ILogger<AccountsApiClient> _logger = logger;

    public Task CreditAccountAsync(CreditAccount request)
        => _httpClient.PostAsJsonAsync($"http://{_config.AccountsApiServiceName}/{_config.CreditAccountEndpoint}", request);

    public Task DebitAccountAsync(DebitAccount request)
        => _httpClient.PostAsJsonAsync($"http://{_config.AccountsApiServiceName}/{_config.DebitAccountEndpoint}", request);
}
