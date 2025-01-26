using SavingsPlatform.Contracts.Accounts.Models;

namespace SavingsPlatform.Accounts.ApiClients;

public interface IEventStoreApiClient
{
    Task<IDictionary<string, AccountBalanceRangeEntry>> GetBalancesForAccountHierarchyAsync
        (string currentAccountId, DateTime? fromDate, DateTime? toDate);
}
