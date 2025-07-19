using Dapr.Client;
using Microsoft.Extensions.Options;
using SavingsPlatform.Common.Config;
using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Models;
using SavingsPlatform.Contracts.Accounts.Requests;

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

    public async Task<ICollection<BaseAccountResponse>> GetAllUserAccountsAsync(string userId)
    {
        var accHolderDetails = await _daprClient.InvokeMethodAsync<AccountHolderResponse>(
                 HttpMethod.Get,
                 _config.AccountsApiServiceName,
                 string.Format(_config.AccountHoldersEndpoint, userId));

        if (accHolderDetails is not null)
        {
            var accounts = await _daprClient.InvokeMethodAsync<ICollection<string>, ICollection<BaseAccountResponse>>(
                _config.AccountsApiServiceName,
                _config.AccountsByIdsEndpoint,
                [.. accHolderDetails.Accounts.Select(x => x.AccountId)]);

            return accounts;
        }
        return Enumerable.Empty<BaseAccountResponse>().ToList();
    }

    public async Task AddCurrentAccountAsync(string externalRef, Currency currency, string userId)
    {
        var req = _daprClient.CreateInvokeMethodRequest<CreateCurrentAccount>(
            HttpMethod.Post,
            _config.AccountsApiServiceName,
            _config.CreateCurrentAccountEndpoint,
            null, 
            new CreateCurrentAccount(externalRef, currency, userId));

        var result = await _daprClient.InvokeMethodWithResponseAsync(req);

        if (result.StatusCode != System.Net.HttpStatusCode.Accepted)
        {
            throw new InvalidOperationException($"Failed to create current account with externalRef = {externalRef}");
        }

    }
}
