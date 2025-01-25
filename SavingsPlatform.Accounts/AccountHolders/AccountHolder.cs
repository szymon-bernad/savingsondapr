using Microsoft.Extensions.Logging;
using SavingsPlatform.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavingsPlatform.Accounts.AccountHolders
{
    public class AccountHolder(IStateEntryRepository<AccountHolderState> repository, AccountHolderState? state, ILogger<AccountHolder> logger) 
        : IAggregateRoot<AccountHolderState>
    {
        private AccountHolderState? _state = state;
        private readonly IStateEntryRepository<AccountHolderState> _repository = repository;
        private readonly ILogger<AccountHolder> _logger = logger;

        public async Task Create(string key, string? username, string externalRef, string[] accountIds)
        {
            var state = new AccountHolderState
            {
                Key = key,
                Username = username,
                ExternalRef = externalRef,
                AccountIds = accountIds,
            };

            var exists = (await _repository.GetAccountAsync(externalRef)) is not null;

            if (exists)
            {
                throw new InvalidOperationException($"AccountHolder already exists");
            }

            await _repository.AddAccountAsync(state);
        }

        public Task AddAccount(string accountId)
        {
            if (_state is null)
            {
                throw new InvalidOperationException($"AccountHolder does not exist");
            }

            if(_state.AccountIds.Contains(accountId))
            {
                _logger.LogWarning($"Account {accountId} already assigned to AccountHolder {_state.Key}");
                return Task.CompletedTask;
            }

            _state.AccountIds.Add(accountId);
            return _repository.TryUpdateAccountAsync(_state, null);
        }

        public AccountHolderState? State => _state;
    }
}
