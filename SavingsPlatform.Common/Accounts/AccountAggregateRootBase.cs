using Microsoft.Extensions.Logging;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Commands;
using SavingsPlatform.Contracts.Accounts.Events;
using System.Collections.ObjectModel;
using static Google.Rpc.Context.AttributeContext.Types;

namespace SavingsPlatform.Common.Accounts
{
    public class AccountAggregateRootBase<T> : IAggregateRoot<T> where T : IAccountAggregateStateEntry
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

        protected ICollection<object> PrepareForCredit(decimal amount, string? transferRef, string? msgId)
        {
            if (_state is null)
            {
                _logger?.LogError($"{nameof(PrepareForCredit)}: Account with invalid state.");
                throw new ApplicationException($"Account with invalid state.");
            }

            if (amount <= 0m)
            {
                _logger?.LogError($"{nameof(PrepareForCredit)}: Credit transaction amount must be greater than 0.00");
                throw new InvalidOperationException($"Credit transaction amount must be greater than 0.00");
            }

            var transactionId = Guid.NewGuid();
            var eventsToPublish = _state.UnpublishedEvents?.Any() ?? false ?
                new Collection<object>([.. _state.UnpublishedEvents]) :
                [];

            eventsToPublish.Add(new AccountCredited(
                Guid.NewGuid().ToString(),
                _state.ExternalRef,
                _state.Key,
                amount,
                msgId,
                transferRef,
                DateTime.UtcNow,
                _state.TotalBalance + amount,
                typeof(AccountCredited)!.Name,
                _state.Type,
                _state.CurrentAccountId));

            return eventsToPublish;
        }

        protected ICollection<object>? PrepareForDebit(decimal amount, string? transferRef, string? msgId)
        {
            if (_state is null)
            {
                _logger?.LogError($"{nameof(PrepareForDebit)}: Account with invalid state.");
                throw new ApplicationException($"Account with invalid state.");
            }
            if (amount <= 0m)
            {
                _logger?.LogError($"{nameof(PrepareForDebit)}: Debiy transaction amount must be greater than 0.00");
                throw new InvalidOperationException($"Debiy transaction amount must be greater than 0.00");
            }

            if (_state.TotalBalance < amount)
            {
                _logger?.LogError($"{nameof(PrepareForDebit)}: Account with {nameof(_state.Key)} = {_state.Key} has insufficient funds.");
                throw new InvalidOperationException(
                    $"Account with {nameof(_state.Key)} = {_state.Key}" +
                    $" has insufficient funds.");
            }

            return [ new AccountDebited(
                Guid.NewGuid().ToString(),
                _state.ExternalRef,
                _state.Key,
                amount,
                msgId,
                transferRef,
                DateTime.UtcNow,
                _state.TotalBalance - amount,
                typeof(AccountDebited)!.Name,
                _state.Type,
                _state.CurrentAccountId)];
        }

        public async Task TryUpdateAsync(string? msgId)
        {
            _logger?.LogInformation("Trying update for {Key} with {MsgId}.", _state!.Key, msgId ?? string.Empty);

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
