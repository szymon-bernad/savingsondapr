using Dapr.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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


    public async Task<IEnumerable<TransactionEntry>> GetTransactionsForAccountHierarchyAsync(string currentAccountId, DateTime? fromDate, DateTime? toDate)
    {
        var queryString = new StringBuilder(string.Format(_config.TransactionsEndpoint, currentAccountId));

        if (fromDate.HasValue)
        {
            queryString.Append($"?{_config.FromQueryParameter}={fromDate.Value:s}");
        }

        if (toDate.HasValue)
        {
            queryString.Append(fromDate.HasValue ? "&" : "?");
            queryString.Append($"{_config.ToQueryParameter}={toDate.Value:s}");
        }

        var daprResult = await _daprClient.InvokeMethodAsync<IEnumerable<TransactionEntry>>(
            HttpMethod.Get,
            _config.EventStoreApiServiceName,
            queryString.ToString());

        return daprResult ?? Enumerable.Empty<TransactionEntry>();
    }
}
