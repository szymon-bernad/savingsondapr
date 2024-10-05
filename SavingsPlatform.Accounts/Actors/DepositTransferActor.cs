using Dapr.Actors.Runtime;
using SavingsPlatform.Accounts.Actors.Services;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Current.Models;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Common.Services;
using SavingsPlatform.Contracts.Accounts.Models;

namespace SavingsPlatform.Accounts.Actors;

public class DepositTransferActor : Actor, IDepositTransferActor, IRemindable
{
    private readonly DepositTransferService _depositTransferService;

    public DepositTransferActor(
        IStateEntryQueryHandler<CurrentAccountState> _currentAccountQueryHandler,
        IStateEntryQueryHandler<InstantAccessSavingsAccountState> _iasaQueryHandler,
        IEventPublishingService eventPublishingService,
        ActorHost host) : base(host)
    {
        _depositTransferService = new (
            StateManager,
            _currentAccountQueryHandler,
            _iasaQueryHandler,
            eventPublishingService);
    }

    public async Task InitiateTransferAsync(DepositTransferData? data)
    {
        var result = await _depositTransferService.InitiateTransferAsync(data);

        var task = result switch
        {
            DepositTransferService.TransferAttemptUnregister => 
                    UnregisterReminderAsync(DepositTransferService.TransferAttempt),
            DepositTransferService.TransferAttemptRegister => 
                    RegisterReminderAsync(
                        DepositTransferService.TransferAttempt,
                        null,
                        TimeSpan.FromMinutes(2),
                        TimeSpan.FromMinutes(2)),
            _ => Task.CompletedTask
        };

        await task;
    }

    public Task HandleDebitedEventAsync(string accountId)
    {
        return _depositTransferService.HandleDebitedEventAsync(accountId);
    }

    public Task HandleCreditedEventAsync(string accountId)
    {
        return _depositTransferService.HandleCreditedEventAsync(accountId);
    }

    public async Task ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period)
    {
        var res = reminderName switch
        {
            DepositTransferService.TransferAttempt => Task.Run(async () =>
            {
                await InitiateTransferAsync(null);
            }),
            _ => Task.CompletedTask
        };

        await res;
    }
}
