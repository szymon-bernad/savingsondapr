using Dapr.Actors.Runtime;
using Microsoft.Extensions.Logging;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Aggregates.InstantAccess;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Common.Services;
using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Models;
using SavingsPlatform.Contracts.Accounts.Requests;
using SavingsPlatform.Accounts.Current;
using SavingsPlatform.Accounts.Current.Models;
using SavingsPlatform.Contracts.Accounts.Commands;

namespace SavingsPlatform.Accounts.Actors;

public class DepositTransferActor : Actor, IDepositTransferActor, IRemindable
{
    private readonly IAggregateRootFactory<CurrentAccount, CurrentAccountState> _currentFactory;
    private readonly IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState> _iasaFactory;
    private readonly IEventPublishingService _eventPublishingService;

    private const string TransferAttempt = nameof(TransferAttempt);
    private const string DepositTransferState = nameof(DepositTransferState);

    public DepositTransferActor(
        IAggregateRootFactory<CurrentAccount, CurrentAccountState> currentFactory,
        IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState> iasaFactory,
        ILogger<DepositTransferActor> logger,
        IEventPublishingService eventPublishingService,
        ActorHost host) : base(host)
    {
        _currentFactory = currentFactory;
        _iasaFactory = iasaFactory;
        _eventPublishingService = eventPublishingService;
    }

    public async Task InitiateTransferAsync(DepositTransferData data)
    {
        if (data.Status != DepositTransferStatus.New)
        {
            await UnregisterReminderAsync(TransferAttempt);
            return;
        }

        await StartTransfer(data);
    }

    public async Task HandleDebitedEventAsync(string accountId)
    {
        var transferData = await StateManager.GetStateAsync<DepositTransferData>(DepositTransferState);
        if (!accountId.Equals(transferData.DebtorAccountId, StringComparison.Ordinal) ||
            transferData.Status != DepositTransferStatus.DebtorDebited)
        {
            return;
        }

        var creditTask = transferData.Direction switch
        {
            TransferType.CurrentToSavings => Task.Run(async () =>
            {
                var instance = await _iasaFactory.GetInstanceAsync(transferData.BeneficiaryAccountId);
                await _eventPublishingService.PublishCommand(
                new CreditAccountCommand(
                        $"{transferData.TransferId}-Credit",
                    instance.State.ExternalRef,
                    transferData.Amount,
                    DateTime.UtcNow,
                    AccountType.SavingsAccount,
                    transferData.TransferId));
            }),
            TransferType.SavingsToCurrent => Task.Run(async () =>
            {
                var instance = await _currentFactory.GetInstanceAsync(transferData.BeneficiaryAccountId);
                await _eventPublishingService.PublishCommand(
                    new CreditAccountCommand(
                        $"{transferData.TransferId}-Credit",
                        instance.State.ExternalRef,
                        transferData.Amount,
                        DateTime.UtcNow,
                        AccountType.CurrentAccount,
                        transferData.TransferId));

            }),
            _ => throw new InvalidOperationException("Unsupported Transfer.")
        };

        await creditTask;
        transferData = transferData with { Status = DepositTransferStatus.BeneficiaryCredited };
        await StateManager.SetStateAsync(DepositTransferState, transferData);
    }


    public async Task HandleCreditedEventAsync(string accountId)
    {
        var transferData = await StateManager.GetStateAsync<DepositTransferData>(DepositTransferState);
        if (!accountId.Equals(transferData.BeneficiaryAccountId, StringComparison.Ordinal) ||
            transferData.Status != DepositTransferStatus.BeneficiaryCredited)
        {
            return;
        }
        transferData = transferData with { Status = DepositTransferStatus.Completed };
        await StateManager.SetStateAsync(DepositTransferState, transferData);
    }

    private Task StartTransfer(DepositTransferData transferData)
    {
        return transferData.Direction switch
        {
            TransferType.CurrentToSavings => StartTransferCurrentToSavings(transferData),
            TransferType.SavingsToCurrent => StartTransferSavingsToCurrent(transferData),
            _ => throw new InvalidOperationException("Unsupported Transfer.")
        };
    }

    private async Task StartTransferCurrentToSavings(DepositTransferData transferData)
    {
        var registerReminder = false;
        var currentAccount = await _currentFactory.GetInstanceAsync(transferData.DebtorAccountId);

        if (currentAccount.State!.TotalBalance < transferData.Amount)
        {
            registerReminder = true;
        }
        else
        {
            await _eventPublishingService.PublishCommand(
                new DebitAccountCommand(
                        $"{transferData.TransferId}-Debit",
                    currentAccount.State.ExternalRef,
                    transferData.Amount,
                    DateTime.UtcNow,
                    AccountType.CurrentAccount,
                    transferData.TransferId));

            transferData = transferData with { Status = DepositTransferStatus.DebtorDebited };

            await StateManager.SetStateAsync(DepositTransferState, transferData);
            await UnregisterReminderAsync(TransferAttempt);
        }

        if (transferData.IsFirstAttempt && registerReminder)
        {
            transferData = transferData with { IsFirstAttempt = false };

            await StateManager.SetStateAsync(DepositTransferState, transferData);

            await RegisterReminderAsync(
                TransferAttempt,
                null,
                TimeSpan.FromMinutes(2),
                TimeSpan.FromMinutes(2));
        }
    }

    private async Task StartTransferSavingsToCurrent(DepositTransferData transferData)
    {
        var savingsAccount = await _iasaFactory.GetInstanceAsync(transferData.DebtorAccountId);

        if (savingsAccount.State!.TotalBalance < transferData.Amount)
        {
            throw new InvalidOperationException($"Cannot withdraw more than savings account balance");
        }
        else
        {
            await _eventPublishingService.PublishCommand(
                new DebitAccountCommand(
                    $"{transferData.TransferId}-Debit",
                    savingsAccount.State!.ExternalRef!,
                    transferData.Amount,
                    DateTime.UtcNow,
                    AccountType.SavingsAccount,
                    transferData.TransferId));

            transferData = transferData with { Status = DepositTransferStatus.DebtorDebited };
            await StateManager.SetStateAsync(DepositTransferState, transferData);
        }
    }

    public async Task ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period)
    {
        var res = reminderName switch
        {
            TransferAttempt => Task.Run(async() =>
            {
                var transferData = await StateManager.GetStateAsync<DepositTransferData>(DepositTransferState);
                await InitiateTransferAsync(transferData);
            }),
            _ => Task.CompletedTask
        };

        await res;
    }
}
