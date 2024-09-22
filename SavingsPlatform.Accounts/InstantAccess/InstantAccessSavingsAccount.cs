using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Common.Accounts;
using SavingsPlatform.Common.Config;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Commands;
using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Events;
using SavingsPlatform.Contracts.Accounts.Requests;
using System.Collections.ObjectModel;

namespace SavingsPlatform.Accounts.Aggregates.InstantAccess;

public class InstantAccessSavingsAccount : AccountAggregateRootBase<InstantAccessSavingsAccountState>
{
    private readonly SimulationConfig _simulationConfig;

    public InstantAccessSavingsAccount(
        IStateEntryRepository<InstantAccessSavingsAccountState> repository,
        SimulationConfig simulationConfig,
        InstantAccessSavingsAccountState? state = default)
        : base(repository, state)
    {
        _simulationConfig = simulationConfig;
    }

    public async Task CreateAsync(CreateInstantSavingsAccountCommand request)
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
                AccountType = AccountType.SavingsAccount,
                Timestamp = DateTime.UtcNow,
                EventType = typeof(AccountCreated).Name,
                PlatformId = request.PlatformId,
            }
        };

        var state = new InstantAccessSavingsAccountState
        {
            Key = accountId,
            ExternalRef = request.ExternalRef,
            OpenedOn = DateTime.UtcNow,
            InterestRate = request.InterestRate,
            TotalBalance = 0m,
            AccruedInterest = 0m,
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

        ValidateForCredit(request.Amount);

        var transactionId = Guid.NewGuid();
        var eventsToPublish = _state.UnpublishedEvents?.Any() ?? false ?
            new Collection<object>([.. _state.UnpublishedEvents]) :
            [];

        if (_state!.TotalBalance == 0m)
        {
            _state = _state with 
            {
                ActivatedOn = DateTime.UtcNow,
                InterestApplicationDueOn = CalculateInterestApplicationDate(DateTime.UtcNow),
            };

            eventsToPublish.Add(
                new InstantAccessSavingsAccountActivated(
                    Guid.NewGuid().ToString(),
                    _state.ExternalRef,
                    _state.Key,
                    request.Amount,
                    _state.InterestRate,
                    DateTime.UtcNow,
                    typeof(InstantAccessSavingsAccountActivated)!.Name,
                    _state.PlatformId));
        }

        eventsToPublish.Add(new AccountCredited(
                Guid.NewGuid().ToString(),
                _state.ExternalRef,
                _state.Key,
                request.Amount,
                request.TransferRef,
                DateTime.UtcNow,
                typeof(AccountCredited)!.Name,
                _state.Type,
                _state.PlatformId));

        _state = _state with
        {
            LastTransactionId = transactionId,
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

        ValidateForDebit(request.Amount, _state.TotalBalance);

        var transactionId = Guid.NewGuid();
        var eventsToPublish = _state.UnpublishedEvents?.Any() ?? false ?
            new Collection<object>([.. _state.UnpublishedEvents]) :
            [];
        eventsToPublish.Add(new AccountDebited(
                    Guid.NewGuid().ToString(),
                    _state.ExternalRef,
                    _state.Key,
                    request.Amount,
                    request.TransferRef,
                    DateTime.UtcNow,
                    typeof(AccountDebited)!.Name,
                    _state.Type,
                    _state.PlatformId));

        _state = _state with
        {
            LastTransactionId = transactionId,
            TotalBalance = _state.TotalBalance - request.Amount,
            HasUnpublishedEvents = true,
            UnpublishedEvents = eventsToPublish,
        };

        await TryUpdateAsync(request.MsgId);
    }

    public async Task AccrueInterest(DateTime? since, DateTime till)
    {
        if (_state is null)
        {
            throw new InvalidOperationException($"AccrueInterest: Cannot process null state");
        }

        var actualSince = (since ?? _state.ActivatedOn) ?? 
            throw new InvalidOperationException(
                $"AccrueInterest: Cannot calculate interest " +
                $"because of null {nameof(since)} and {nameof(_state.ActivatedOn)}.");

        var tsDays = this.CalculateDaysCount(actualSince, till);

        if(tsDays > 0)
        {
            var accrualRatio = (tsDays / 365.0m) * (0.01m * _state.InterestRate);
            var toBeAccrued = Math.Round(accrualRatio * _state.TotalBalance, 2, MidpointRounding.ToEven);
            if (toBeAccrued > 0m)
            {
                var eventsToPublish = _state.UnpublishedEvents?.Any() ?? false ?
                    new Collection<object>(_state.UnpublishedEvents.ToList()) :
                    new Collection<object>();
                eventsToPublish.Append(
                    new AccountInterestAccrued(
                        Guid.NewGuid().ToString(),
                        _state.ExternalRef,
                        _state.Key,
                        _state.AccruedInterest + toBeAccrued,
                        _state.TotalBalance,
                        _state.InterestRate,
                        DateTime.UtcNow,
                        typeof(AccountInterestAccrued)!.Name,
                        _state.PlatformId));

                _state = _state with
                {
                    AccruedInterest = _state.AccruedInterest + toBeAccrued,
                    HasUnpublishedEvents = true,
                    UnpublishedEvents = eventsToPublish,
                };

                TryApplyInterest(eventsToPublish);
                await TryUpdateAsync(null);
            }
        }  
    }

    private int CalculateDaysCount(DateTime actualSince, DateTime till)
    {
        if (_simulationConfig.SpeedMultiplier > 1)
        {
            var tsDays = ((till - actualSince).TotalMinutes * _simulationConfig.SpeedMultiplier) / 1440;

            return (int)Math.Floor(tsDays);
        }
        else return (till.Date - actualSince.Date).Days;
    }

    private void TryApplyInterest(ICollection<object> eventsToPublish)
    {
        if (_state?.InterestApplicationDueOn is null)
        {
            return;
        }

        if (_state.InterestApplicationDueOn!.Value.Date <= DateTime.UtcNow)
        {
            var evnts = eventsToPublish.Append(
                new AccountInterestApplied(
                    Guid.NewGuid().ToString(),
                    _state.ExternalRef,
                    _state.Key,
                    0m,
                    _state.TotalBalance + _state.AccruedInterest,
                    _state.InterestRate,
                    DateTime.UtcNow,
                    typeof(AccountInterestAccrued)!.Name,
                    _state.PlatformId));

            _state = _state with
            {
                InterestApplicationDueOn = CalculateInterestApplicationDate(_state.InterestApplicationDueOn.Value),
                TotalBalance = _state.TotalBalance + _state.AccruedInterest,
                AccruedInterest = 0m,
                HasUnpublishedEvents = true,
                UnpublishedEvents = evnts.ToList(),
            };
        }
    }

    private DateTime CalculateInterestApplicationDate(DateTime startDate)
    {
        if (_state is null)
        {
            throw new InvalidOperationException($"InterestApplicationDate: state cannot be null");
        }

        return _state!.InterestApplicationFrequency switch
        {
            ProcessFrequency.Daily => startDate.AddDays(1),
            ProcessFrequency.Weekly => startDate.AddDays(7),
            ProcessFrequency.Monthly => startDate.AddMonths(1),
            ProcessFrequency.Yearly => startDate.AddYears(1),
            _ => startDate,
        } ;
    }
}
