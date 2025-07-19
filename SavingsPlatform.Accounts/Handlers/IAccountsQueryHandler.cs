using SavingsPlatform.Contracts.Accounts;
using SavingsPlatform.Contracts.Accounts.Models;

namespace SavingsPlatform.Accounts.Handlers;

public interface IAccountsQueryHandler
{
    Task<ICollection<AccountInfo>> FetchAccountInfosByIds(string[] accountIds);

    Task<ICollection<BaseAccountResponse>> FetchAccountsByIds(string[] accountIds);
}
