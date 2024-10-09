﻿using SavingsPlatform.Contracts.Accounts;

namespace SavingsPlatform.Common.Interfaces;

public interface IStateEntryQueryHandler<T> where T : IAggregateStateEntry
{
    Task<T?> GetAccountAsync(string key);
    Task<ICollection<T>> QueryAccountsByKeyAsync(string[] keyName, object[] keyValue);
}

public interface IStateEntryRepository<T> : IStateEntryQueryHandler<T> where T : IAggregateStateEntry
{
    Task AddAccountAsync(T account);
    Task TryUpdateAccountAsync(T account, MessageProcessedEntry? msgEntry);
    Task<bool> IsMessageProcessed(string msgId);
}
