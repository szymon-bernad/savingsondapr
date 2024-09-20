using SavingsPlatform.Contracts.Accounts;

namespace SavingsPlatform.Common.Interfaces
{
    public interface IStateEntryRepository<T> where T : IAggregateStateEntry
    {
        Task<T?> GetAccountAsync(string key);
        Task<ICollection<T>> QueryAccountsByKeyAsync(string[] keyName, string[] keyValue, bool isKeyValueAString = true);
        Task AddAccountAsync(T account);
        Task TryUpdateAccountAsync(T account, MessageProcessedEntry? msgEntry);
        Task<bool> IsMessageProcessed(string msgId);
    }
}
