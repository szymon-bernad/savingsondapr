using SavingsPlatform.Contracts.Accounts.Models;

namespace SavingsPlatform.Accounts.ApiClients;

public interface IEventStoreApiClient
{
    Task<IEnumerable<TransactionEntry>> GetTransactionsForAccountHierarchyAsync(string currentAccountId, DateTime? fromDate, DateTime? toDate);
}
