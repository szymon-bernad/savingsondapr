using Microsoft.Extensions.Logging;
using NetTopologySuite.Triangulate.Tri;
using SavingsPlatform.Accounts.Current;
using SavingsPlatform.Common.Interfaces;
using static FastExpressionCompiler.ExpressionCompiler;

namespace SavingsPlatform.Accounts.AccountHolders;

internal class AccountHolderFactory(IStateEntryRepository<AccountHolderState> repo,
                                    ILogger<AccountHolder> logger) 
        : IAggregateRootFactory<AccountHolder, AccountHolderState>
{
    private readonly IStateEntryRepository<AccountHolderState> _repository = repo;
    private readonly ILogger<AccountHolder> _logger = logger;

    public async Task<AccountHolder> GetInstanceAsync(string? id = null)
    {
        if (id is null)
        {
            return new AccountHolder(_repository, null, _logger);
        }

        var stateEntry = await _repository.GetAccountAsync(id);

        return stateEntry is null
            ? throw new InvalidOperationException($"Account with {id} not found")
            : new AccountHolder(_repository, stateEntry, _logger);
    }

    public Task<AccountHolder> GetInstanceByExternalRefAsync(string externalRef)
    {
        throw new NotImplementedException();
    }

    public async Task<AccountHolder?> TryGetInstanceAsync(string id)
    {
        if (string.IsNullOrEmpty(id?.Trim()))
        {
            throw new InvalidOperationException($"{nameof(id)} cannot be null or empty");
        }

        var result = await _repository.GetAccountAsync(id);

        return result is not null ?
            new AccountHolder(_repository, result, _logger) :
            null;
    }

    public Task<AccountHolder?> TryGetInstanceByExternalRefAsync(string externalRef)
    {
        throw new NotImplementedException();
    }
}
