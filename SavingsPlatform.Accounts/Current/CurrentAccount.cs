using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Current.Models;
using SavingsPlatform.Common.Accounts;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Commands;
using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Events;
using System.Collections.ObjectModel;

namespace SavingsPlatform.Accounts.Current;

public class CurrentAccount : AccountAggregateRootBase<CurrentAccountState>
{
    public CurrentAccount(
        IStateEntryRepository<CurrentAccountState> repository,
        CurrentAccountState? state = default)
        : base(repository, state)
    {

    }

    public async Task CreateAsync(CreateCurrentAccountCommand request)
    {
        if (request is null || string.IsNullOrEmpty(request.ExternalRef))
        {
            throw new InvalidOperationException($"{nameof(request.ExternalRef)} cannot be null");
        }

        if (_state is not null)
        {
            throw new InvalidOperationException(
                $"SavingsAccount with {nameof(InstantAccessSavingsAccountState.ExternalRef)} = {request.ExternalRef}" +
                $" already exists");
        }

        var accountId = Guid.NewGuid().ToString();

        await ThrowIfAlreadyExists(accountId, request.ExternalRef);

        var eventsToPub = new Collection<object>
        {
            new AccountCreated
            {
                Id = Guid.NewGuid().ToString(),
                ExternalRef = request.ExternalRef,
                AccountId = accountId,
                AccountType = AccountType.CurrentAccount,
                Timestamp = DateTime.UtcNow,
                EventType = typeof(AccountCreated).Name,
                PlatformId = request.PlatformId,
            }
        };

        var state = new CurrentAccountState
        {
            Key = accountId,
            ExternalRef = request.ExternalRef,
            OpenedOn = DateTime.UtcNow,
            TotalBalance = 0m,
            PlatformId = request.PlatformId,
            HasUnpublishedEvents = true,
            UnpublishedEvents = eventsToPub,
        };

        await CreateAsync(state);
    }

    public async Task CreditAsync(CreditAccountCommand request)
    {
        if (_state is null)
        {
            throw new InvalidOperationException($"Credit: state cannot be null");
        }

        if (await this.ValidateIfProcessedAsync(request.MsgId))
        {
            return;
        }

        var eventsToPublish = PrepareForCredit(request.Amount, request.TransferRef);

        _state = _state with
        {
            TotalBalance = _state.TotalBalance + request.Amount,
            HasUnpublishedEvents = true,
            UnpublishedEvents = eventsToPublish,
        };

        await TryUpdateAsync(request.MsgId);
    }

    public async Task DebitAsync(DebitAccountCommand request)
    {
        if (_state is null)
        {
            throw new InvalidOperationException($"Debit: state cannot be null");
        }

        if (await this.ValidateIfProcessedAsync(request.MsgId))
        {
            return;
        }

        var eventsToPublish = PrepareForDebit(request.Amount, request.TransferRef);

        _state = _state with
        {
            TotalBalance = _state.TotalBalance - request.Amount,
            HasUnpublishedEvents = true,
            UnpublishedEvents = eventsToPublish,
        };

        await TryUpdateAsync(request.MsgId);
    }
}
