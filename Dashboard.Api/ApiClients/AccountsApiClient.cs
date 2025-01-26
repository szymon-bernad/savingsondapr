using Dapr.Client;
using Microsoft.Extensions.Options;
using SavingsPlatform.Common.Config;
using SavingsPlatform.Contracts.Accounts.Models;

namespace Dashboard.Api.ApiClients;

public class AccountsApiClient(IOptions<AccountsApiConfig> config,
                                    DaprClient daprClient)
    : IAccountsApiClient
{
    private readonly AccountsApiConfig _config = config.Value
            ?? throw new ArgumentNullException(nameof(config));
    private readonly DaprClient _daprClient = daprClient;

   public Task<AccountHolderResponse> GetAccountHolderDetailsAsync(string userId)
        => _daprClient.InvokeMethodAsync<AccountHolderResponse>(
                HttpMethod.Get,
                _config.AccountsApiServiceName,
                string.Format(_config.AccountHoldersEndpoint, userId));

    public async Task<ICollection<CurrentAccountResponse>> GetAllUserAccountsAsync(string userId)
    {
        var accHolderDetails = await _daprClient.InvokeMethodAsync<AccountHolderResponse>(
                 HttpMethod.Get,
                 _config.AccountsApiServiceName,
                 string.Format(_config.AccountHoldersEndpoint, userId));

        if (accHolderDetails is not null)
        {
            var accounts = await _daprClient.InvokeMethodAsync<ICollection<string>, ICollection<CurrentAccountResponse>>(
                _config.AccountsApiServiceName,
                _config.AccountsByIdsEndpoint,
                accHolderDetails.AccountIds);
        }
        return Enumerable.Empty<CurrentAccountResponse>().ToList();
    }
}
