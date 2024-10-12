using Dapr.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SavingsPlatform.Accounts.Current;
using SavingsPlatform.Common.Config;
using SavingsPlatform.Contracts.Accounts.Models;
using System.Text;

namespace SavingsPlatform.Accounts.ApiClients;

public class EventStoreApiClient(
    IOptions<EventStoreApiConfig> cfgOptions,
    DaprClient daprClient,
    ILogger<EventStoreApiClient> logger
    ) : IEventStoreApiClient
{
    private readonly EventStoreApiConfig _config = cfgOptions.Value 
                ?? throw new ArgumentNullException(nameof(cfgOptions));
    private readonly DaprClient _daprClient = daprClient;
    private readonly ILogger<EventStoreApiClient> _logger = logger;


    public Task<IDictionary<string, AccountBalanceRangeEntry>> GetBalancesForAccountHierarchyAsync(
        string currentAccountId, DateTime? fromDate, DateTime? toDate) =>
            _daprClient.InvokeMethodAsync<IDictionary<string, AccountBalanceRangeEntry>>(
                HttpMethod.Get,
                _config.EventStoreApiServiceName,
                PrepareDateRangeQuery(currentAccountId, fromDate, toDate));

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
