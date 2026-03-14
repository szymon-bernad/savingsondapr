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
    private readonly HttpClient _httpClient = daprClient.CreateInvokableHttpClient();

    public Task<AccountHolderResponse?> GetAccountHolderDetailsAsync(string userId)
        => _httpClient.GetFromJsonAsync<AccountHolderResponse>($"http://{_config.AccountsApiServiceName}/{string.Format(_config.AccountHoldersEndpoint, userId)}");


    public async Task<ICollection<BaseAccountResponse>> GetAllUserAccountsAsync(string userId)
    {
        var accHolderDetails = await _httpClient.GetFromJsonAsync<AccountHolderResponse>($"http://{_config.AccountsApiServiceName}/{string.Format(_config.AccountHoldersEndpoint, userId)}");

        if (accHolderDetails is not null)
        {
            var response = await _httpClient.PostAsJsonAsync<ICollection<string>>($"http://{_config.AccountsApiServiceName}/{_config.AccountsByIdsEndpoint}", [.. accHolderDetails.Accounts.Select(x => x.AccountId)]);
            response.EnsureSuccessStatusCode();

            var accounts = await response.Content.ReadFromJsonAsync<ICollection<BaseAccountResponse>>();

            return accounts ?? Enumerable.Empty<BaseAccountResponse>().ToList();
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
                [],
                new CreateCurrentAccount(request.ExternalRef, request.AccountCurrency, request.UserId));
        }
        else if (request.Type == AccountType.SavingsAccount)
        {
            this.ValidateRequestForSavings(request);

            req = _daprClient.CreateInvokeMethodRequest<CreateSavingsAccount>(
                HttpMethod.Post,
                _config.AccountsApiServiceName,
                _config.CreateSavingsAccountEndpoint,
                [],
                new CreateSavingsAccount(
                    request.ExternalRef,
                    request.SavingsDetails!.InterestRate,
                    request.SavingsDetails!.CurrentAccountRef, 
                    request.AccountCurrency,
                    request.UserId));
        }


        var result = await _httpClient.SendAsync(req!);

        if (result.StatusCode != System.Net.HttpStatusCode.Accepted)
        {
            throw new InvalidOperationException($"Failed to create current account with externalRef = {request.ExternalRef}");
        }

    }

    public Task<CurrencyRateResponse?> GetSavingsInterestRateAsync(Currency accountCurrency)
        => _httpClient.GetFromJsonAsync<CurrencyRateResponse>($"http://{_config.AccountsApiServiceName}/{string.Format(_config.SavingsCurrentRateEndpoint, accountCurrency.ToString())}");


    private void ValidateRequestForSavings(CreateAccountRequest request)
    {
        if (request.SavingsDetails is null)
        {
            throw new ArgumentException($"{nameof(SavingsAccountDetails)} must be provided for Savings Account creation");
        }

        if (string.IsNullOrEmpty(request.SavingsDetails.CurrentAccountRef))
        {
            throw new ArgumentException($"{nameof(request.SavingsDetails.CurrentAccountRef)} must be provided for Savings Account creation");
        }


        if (request.SavingsDetails.InterestRate < 0.0001m)
        {
            throw new ArgumentException($"{nameof(request.SavingsDetails.InterestRate)} must be a valid decimal value (greater than 0.0001)");
        }
    }
}
