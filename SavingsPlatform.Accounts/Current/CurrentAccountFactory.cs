using Microsoft.Extensions.Logging;
using SavingsPlatform.Accounts.Current.Models;
using SavingsPlatform.Common.Interfaces;

namespace SavingsPlatform.Accounts.Current;

internal class CurrentAccountFactory : IAggregateRootFactory<CurrentAccount, CurrentAccountState>
{
    private readonly IStateEntryRepository<CurrentAccountState> _repository;
    private readonly ILogger<CurrentAccount> _logger;

    public CurrentAccountFactory(
        IStateEntryRepository<CurrentAccountState> repo,
        ILogger<CurrentAccount> logger)
    {
        _repository = repo;
        _logger = logger;
    }

    public async Task<CurrentAccount> GetInstanceAsync(string? id = null)
    {
        if (id is null)
        {
            return new CurrentAccount(_repository, null, _logger);
        }

        var stateEntry = await _repository.GetAccountAsync(id);

        return stateEntry is null
            ? throw new InvalidOperationException($"Account with {id} not found")
            : new CurrentAccount(_repository, stateEntry, _logger);
    }

    public async Task<CurrentAccount> GetInstanceByExternalRefAsync(string externalRef)
    {
        var stateEntry = (await _repository.QueryAccountsByKeyAsync(["externalRef"], [externalRef])).SingleOrDefault();
        if (stateEntry is not null)
        {
            return new CurrentAccount(_repository, stateEntry, _logger);
        }

        throw new InvalidOperationException($"Cannot get instance with externalRef = {externalRef}");
    }

    public async Task<CurrentAccount?> TryGetInstanceAsync(string id)
    {
        if (string.IsNullOrEmpty(id?.Trim()))
        {
            throw new InvalidOperationException($"{nameof(id)} cannot be null or empty");
        }

        var result = await _repository.GetAccountAsync(id);

        return result is not null ?
            new CurrentAccount(_repository, result, _logger) :
            null;
    }

    public async Task<CurrentAccount?> TryGetInstanceByExternalRefAsync(string externalRef)
    {
        var stateEntry = (await _repository.QueryAccountsByKeyAsync(["externalRef"], [externalRef])).SingleOrDefault();
        if (stateEntry is not null)
        {
            return new CurrentAccount(_repository, stateEntry, _logger);
        }

        return null;
    }
}
