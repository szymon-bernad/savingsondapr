using Dapr.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SavingsPlatform.Accounts.Current;
using SavingsPlatform.Common.Config;
using SavingsPlatform.Contracts.Accounts.Models;
using System.Net.Http.Json;
using System.Text;

namespace SavingsPlatform.Accounts.ApiClients;

public class EventStoreApiClient(
    IOptions<EventStoreApiConfig> cfgOptions,
    DaprClient daprClient,
    HttpClient httpClient,
    ILogger<EventStoreApiClient> logger
    ) : IEventStoreApiClient
{
    private readonly EventStoreApiConfig _config = cfgOptions.Value
                ?? throw new ArgumentNullException(nameof(cfgOptions));
    private readonly DaprClient _daprClient = daprClient;
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<EventStoreApiClient> _logger = logger;


    public async Task<IDictionary<string, AccountBalanceRangeEntry>> GetBalancesForAccountHierarchyAsync(
        string currentAccountId, DateTime? fromDate, DateTime? toDate)
    {
        var req = _daprClient.CreateInvokeMethodRequest(
                HttpMethod.Get,
                _config.EventStoreApiServiceName,
                PrepareDateRangeQuery(currentAccountId, fromDate, toDate));

        var response = await _httpClient.SendAsync(req);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<IDictionary<string, AccountBalanceRangeEntry>>()
            ?? throw new InvalidOperationException("Failed to deserialize account balances response.");
    }

    private string PrepareDateRangeQuery(string currentAccountId, DateTime? fromDate, DateTime? toDate)
    {
        var queryString = new StringBuilder(string.Format(_config.BalancesEndpoint, currentAccountId));

        if (fromDate.HasValue)
        {
            queryString.Append($"?{_config.FromQueryParameter}={fromDate.Value:s}");
        }

        if (toDate.HasValue)
        {
            queryString.Append(fromDate.HasValue ? "&" : "?");
            queryString.Append($"{_config.ToQueryParameter}={toDate.Value:s}");
        }

        return queryString.ToString();
    }
}
