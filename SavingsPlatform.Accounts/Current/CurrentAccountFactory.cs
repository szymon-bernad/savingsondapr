using SavingsPlatform.Accounts.Aggregates.InstantAccess;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Current.Models;
using SavingsPlatform.Common.Config;
using SavingsPlatform.Common.Interfaces;

namespace SavingsPlatform.Accounts.Current;

internal class CurrentAccountFactory : IAggregateRootFactory<CurrentAccount, CurrentAccountState>
{
    private readonly IStateEntryRepository<CurrentAccountState> _repository;

    public CurrentAccountFactory(IStateEntryRepository<CurrentAccountState> repo)
    {
        _repository = repo;
    }

    public async Task<CurrentAccount> GetInstanceAsync(string? id = null)
    {
        if (id is null)
        {
            return new CurrentAccount(_repository, null);
        }

        var stateEntry = await _repository.GetAccountAsync(id);

        return stateEntry is null
            ? throw new InvalidOperationException($"Account with {id} not found")
            : new CurrentAccount(_repository, stateEntry);
    }

    public async Task<CurrentAccount> GetInstanceByExternalRefAsync(string externalRef)
    {
        var stateEntry = (await _repository.QueryAccountsByKeyAsync(["data.externalRef"], [externalRef])).SingleOrDefault();
        if (stateEntry is not null)
        {
            return new CurrentAccount(_repository, stateEntry);
        }
        
        throw new InvalidOperationException($"Cannot get instance with externalRef = {externalRef}");
    }
}
