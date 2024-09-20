using Microsoft.Extensions.Logging;
using SavingsPlatform.Common.Interfaces;

namespace SavingsPlatform.Common.Accounts
{
    public class AccountAggregateRootBase<T> : IAggregateRoot<T> where T : IAggregateStateEntry
    {
        protected T? _state;
        protected readonly IStateEntryRepository<T> _repository;
        protected readonly ILogger? _logger;

        public AccountAggregateRootBase(
            IStateEntryRepository<T> repository,
            T? state = default,
            ILogger? logger = null)
        {
            _repository = repository;
            _state = state;
            _logger = logger;
        }

        protected async Task CreateAsync(T state)
        {
            await _repository.AddAccountAsync(state);

            _state = await _repository.GetAccountAsync(state.Key);

            if (_state is null)
            {
                _logger?.LogError($"Could not create account with {nameof(state.Key)} = {state.Key}");

                throw new ApplicationException(
                    $"Account has not been persisted in the state store.");
            }
        }

        protected async Task ThrowIfAlreadyExists(string key, string externalRef)
        {
            var res = await _repository.GetAccountAsync(key);

            if (res is not null)
            {
                throw new InvalidOperationException(
                    $"Account with {nameof(_state.Key)} = {key} already exists");
            }

            var queryRes = await _repository.QueryAccountsByKeyAsync(new string[] { "data.externalRef" }, new string[] { externalRef });
            if (queryRes.Any())
            {
                throw new InvalidOperationException(
                    $"Account with {nameof(_state.ExternalRef)} = {externalRef} already exists");
            }
        }

        protected void ValidateForCredit(decimal amount)
        {
            if (_state is null)
            {
                _logger?.LogError($"{nameof(ValidateForCredit)}: Account with invalid state.");
                throw new ApplicationException($"Account with invalid state.");
            }

            if (amount <= 0m)
            {
                _logger?.LogError($"{nameof(ValidateForCredit)}: Credit transaction amount must be greater than 0.00");
                throw new InvalidOperationException($"Credit transaction amount must be greater than 0.00");
            }
        }

        protected void ValidateForDebit(decimal amount, decimal totalBalance)
        {
            if (_state is null)
            {
                _logger?.LogError($"{nameof(ValidateForDebit)}: Account with invalid state.");
                throw new ApplicationException($"Account with invalid state.");
            }
            if (amount <= 0m)
            {
                _logger?.LogError($"{nameof(ValidateForDebit)}: Debiy transaction amount must be greater than 0.00");
                throw new InvalidOperationException($"Debiy transaction amount must be greater than 0.00");
            }

            if (totalBalance < amount)
            {
                _logger?.LogError($"{nameof(ValidateForDebit)}: Account with {nameof(_state.Key)} = {_state.Key} has insufficient funds.");
                throw new InvalidOperationException(
                    $"Account with {nameof(_state.Key)} = {_state.Key}" +
                    $" has insufficient funds.");
            }
        }

        public async Task TryUpdateAsync(string? msgId)
        {
            var msgEntry = msgId is not null ?
                new Contracts.Accounts.MessageProcessedEntry
                {
                    MessageId = msgId!,
                    ProcessedOn = DateTime.UtcNow
                } 
                : null;

            if (!string.IsNullOrEmpty(msgId) && (await _repository.IsMessageProcessed(msgId)))
            {
                return;
            }

            await _repository.TryUpdateAccountAsync(
                _state!,
                msgEntry);
        }

        protected Task<bool> ValidateIfProcessedAsync(string msgId)
        {
            return _repository.IsMessageProcessed(msgId);
        }

        public T? State => _state;
    }
}
