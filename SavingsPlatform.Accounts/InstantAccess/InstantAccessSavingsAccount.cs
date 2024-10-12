using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Common.Accounts;
using SavingsPlatform.Common.Config;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Commands;
using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Events;
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
            new AccountCreated(
                Guid.NewGuid().ToString(),
                request.ExternalRef,
                request.CurrentAccountId,
                accountId,
                AccountType.SavingsAccount,
                DateTime.UtcNow,
                typeof(AccountCreated).Name)
        };

        var state = new InstantAccessSavingsAccountState
        {
            Key = accountId,
            ExternalRef = request.ExternalRef,
            OpenedOn = DateTime.UtcNow,
            InterestRate = request.InterestRate,
            TotalBalance = 0m,
            AccruedInterest = 0m,
            CurrentAccountId = request.CurrentAccountId,
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

        if (_state!.TotalBalance == 0m)
        {
            _state = _state with 
            {
                ActivatedOn = DateTime.UtcNow,
                InterestApplicationDueOn = CalculateInterestApplicationDate(DateTime.UtcNow),
                InterestAccruedOn = DateTime.UtcNow,
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
                    _state.CurrentAccountId));
        }

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

    public async Task AccrueInterest(DateTime? since, DateTime till, decimal? adjustedBalance)
    {
        if (_state is null)
        {
            throw new InvalidOperationException($"AccrueInterest: Cannot process null state");
        }

        var referredBalance = adjustedBalance ?? _state.TotalBalance;

        var actualSince = (since ?? _state.ActivatedOn) ?? 
            throw new InvalidOperationException(
                $"AccrueInterest: Cannot calculate interest " +
                $"because of null {nameof(since)} and {nameof(_state.ActivatedOn)}.");

        var tsDays = this.CalculateDaysCount(actualSince, till);

        if(tsDays > 0)
        {
            var accrualRatio = (tsDays / 365.0m) * (0.01m * _state.InterestRate);
            var toBeAccrued = Math.Round(accrualRatio * referredBalance, 2, MidpointRounding.ToEven);
            if (toBeAccrued > 0m)
            {
                var eventsToPublish = _state.UnpublishedEvents?.Any() ?? false ?
                    new Collection<object>(_state.UnpublishedEvents.ToList()) :
                    new Collection<object>();

                eventsToPublish.Add(
                    new AccountInterestAccrued(
                        Guid.NewGuid().ToString(),
                        _state.ExternalRef,
                        _state.Key,
                        _state.AccruedInterest + toBeAccrued,
                        _state.TotalBalance,
                        _state.InterestRate,
                        till,
                        typeof(AccountInterestAccrued)!.Name,
                        _state.CurrentAccountId));

                _state = _state with
                {
                    AccruedInterest = _state.AccruedInterest + toBeAccrued,
                    InterestAccruedOn = till,
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

        if (_state.InterestApplicationDueOn.Value.Date <= DateTime.UtcNow)
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
                    _state.CurrentAccountId));

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
