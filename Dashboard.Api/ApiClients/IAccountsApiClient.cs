using SavingsPlatform.Contracts.Accounts.Commands;
using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Models;

namespace Dashboard.Api.ApiClients;

public interface IAccountsApiClient
{
    Task<AccountHolderResponse> GetAccountHolderDetailsAsync(string userId);

    Task<ICollection<BaseAccountResponse>> GetAllUserAccountsAsync(string userId);

    Task AddUserAccountAsync(CreateAccountRequest request);

    Task<CurrencyRateResponse> GetSavingsInterestRateAsync(Currency accountCurrency);
}
