using Microsoft.Extensions.Logging;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts;

namespace SavingsPlatform.Accounts.AccountHolders;

public class AccountHolder
    (IStateEntryRepository<AccountHolderState> repository, 
     AccountHolderState? state, ILogger<AccountHolder> logger) 
    : IAggregateRoot<AccountHolderState>
{
    private AccountHolderState? _state = state;
    private readonly IStateEntryRepository<AccountHolderState> _repository = repository;
    private readonly ILogger<AccountHolder> _logger = logger;

    public async Task Create(string key, string? username, string externalRef, ICollection<AccountInfo> accounts)
    {
        var state = new AccountHolderState
        {
            Key = key,
            Username = username,
            ExternalRef = externalRef,
        };
        state.Accounts.AddRange(accounts);

        var exists = (await _repository.GetAccountAsync(externalRef)) is not null;

        if (exists)
        {
            throw new InvalidOperationException($"AccountHolder already exists");
        }

        await _repository.AddAccountAsync(state);
    }

    public Task AddAccounts(ICollection<AccountInfo> accounts)
    {
        if (_state is null)
        {
            throw new InvalidOperationException($"AccountHolder does not exist");
        }

        var accountsToAdd = accounts.Except(_state.Accounts).Distinct().ToList();

        _state.Accounts.AddRange(accountsToAdd);
        return _repository.TryUpdateAccountAsync(_state, null);
    }

    public AccountHolderState? State => _state;
}
