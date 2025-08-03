using Dapr.Client;
using Microsoft.Extensions.Options;
using SavingsPlatform.Common.Config;
using SavingsPlatform.Contracts.Accounts.Commands;
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

    public async Task AddUserAccountAsync(CreateAccountRequest request)
    {

        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrEmpty(request.ExternalRef))
        {
            throw new ArgumentException($"{nameof(request.ExternalRef)} cannot be null or empty");
        }

        if (string.IsNullOrEmpty(request.UserId))
        {
            throw new ArgumentException($"{nameof(request.UserId)} cannot be null or empty");
        }

        HttpRequestMessage? req = null;

        if (request.Type == AccountType.CurrentAccount)
        {
            req = _daprClient.CreateInvokeMethodRequest<CreateCurrentAccount>(
                HttpMethod.Post,
                _config.AccountsApiServiceName,
                _config.CreateCurrentAccountEndpoint,
                null,
                new CreateCurrentAccount(request.ExternalRef, request.AccountCurrency, request.UserId));
        }
        else if (request.Type == AccountType.SavingsAccount)
        {
            this.ValidateRequestForSavings(request);

            req = _daprClient.CreateInvokeMethodRequest<CreateSavingsAccount>(
                HttpMethod.Post,
                _config.AccountsApiServiceName,
                _config.CreateSavingsAccountEndpoint,
                null,
                new CreateSavingsAccount(
                    request.ExternalRef,
                    request.SavingsDetails.InterestRate,
                    request.SavingsDetails.CurrentAccountRef, 
                    request.AccountCurrency,
                    request.UserId));
        }


        var result = await _daprClient.InvokeMethodWithResponseAsync(req);

        if (result.StatusCode != System.Net.HttpStatusCode.Accepted)
        {
            throw new InvalidOperationException($"Failed to create current account with externalRef = {request.ExternalRef}");
        }

    }

    public Task<CurrencyRateResponse> GetSavingsInterestRateAsync(Currency accountCurrency)
       => _daprClient.InvokeMethodAsync<CurrencyRateResponse>(
            HttpMethod.Get,
            _config.AccountsApiServiceName,
            string.Format(_config.SavingsCurrentRateEndpoint, accountCurrency.ToString()));

    private void ValidateRequestForSavings(CreateAccountRequest request)
    {
        if (request.SavingsDetails is null)
        {
            throw new ArgumentException($"{nameof(SavingsAccountDetails)} must be provided for Savings Account creation");
        }

        if (string.IsNullOrEmpty(request.SavingsDetails.CurrentAccountRef))
        {
            throw new ArgumentException($"{request.SavingsDetails.CurrentAccountRef} must be provided for Savings Account creation");
        }


        if (request.SavingsDetails.InterestRate < 0.0001m)
        {
            throw new ArgumentException($"{request.SavingsDetails.InterestRate} must be a valid decimal value (grater than 0.0001)");
        }
    }
}
