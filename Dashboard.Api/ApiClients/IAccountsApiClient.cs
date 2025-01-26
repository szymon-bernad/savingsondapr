using Dapr.Client;
using SavingsPlatform.Contracts.Accounts.Models;

namespace Dashboard.Api.ApiClients;

public interface IAccountsApiClient
{
    Task<AccountHolderResponse> GetAccountHolderDetailsAsync(string userId);
    Task<ICollection<CurrentAccountResponse>> GetAllUserAccountsAsync(string userId);
}
