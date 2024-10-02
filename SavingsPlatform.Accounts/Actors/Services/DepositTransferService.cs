using Dapr.Actors.Runtime;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Current.Models;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Common.Services;
using SavingsPlatform.Contracts.Accounts.Commands;
using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Models;

namespace SavingsPlatform.Accounts.Actors.Services;

public class DepositTransferService(
    IActorStateManager _actorStateManager,
    IStateEntryQueryHandler<CurrentAccountState> _currentAccountQueryHandler,
    IStateEntryQueryHandler<InstantAccessSavingsAccountState> _iasaQueryHandler,
    IEventPublishingService _eventPublishingService)
{
    public const string TransferAttempt = nameof(TransferAttempt);
    public const string DepositTransferState = nameof(DepositTransferState);
    public const string TransferAttemptRegister = nameof(TransferAttemptRegister);
    public const string TransferAttemptUnregister = nameof(TransferAttemptUnregister);

    public async Task<string?> InitiateTransferAsync(DepositTransferData? data)
    {
        data ??= await _actorStateManager.GetStateAsync<DepositTransferData>(DepositTransferService.DepositTransferState);

        if (data is not null)
        {
            if (data.Status != DepositTransferStatus.New)
            {
                return TransferAttemptUnregister;  
            }
            else
            {
                return await StartTransfer(data);
            }
        }

        return null;
    }

    public Task<string?> StartTransfer(DepositTransferData transferData)
    {
        return transferData.Direction switch
        {
            TransferType.CurrentToSavings => StartTransferCurrentToSavings(transferData),
            TransferType.SavingsToCurrent => StartTransferSavingsToCurrent(transferData),
            _ => throw new InvalidOperationException("Unsupported Transfer.")
        };
    }

    public async Task HandleDebitedEventAsync(string accountId)
    {
        var transferData = await _actorStateManager.GetStateAsync<DepositTransferData>(DepositTransferState);
        if (!accountId.Equals(transferData.DebtorAccountId, StringComparison.Ordinal) ||
            transferData.Status != DepositTransferStatus.DebtorDebited)
        {
            return;
        }

        var creditTask = transferData.Direction switch
        {
            TransferType.CurrentToSavings => Task.Run(async () =>
            {
                var instance = await _iasaQueryHandler.GetAccountAsync(transferData.BeneficiaryAccountId);
                await _eventPublishingService.PublishCommand(
                new CreditAccountCommand(
                        $"{transferData.TransferId}-Credit",
                    instance.ExternalRef,
                    transferData.Amount,
                    DateTime.UtcNow,
                    AccountType.SavingsAccount,
                    transferData.TransferId));
            }),
            TransferType.SavingsToCurrent => Task.Run(async () =>
            {
                var instance = await _currentAccountQueryHandler.GetAccountAsync(transferData.BeneficiaryAccountId);
                await _eventPublishingService.PublishCommand(
                    new CreditAccountCommand(
                        $"{transferData.TransferId}-Credit",
                        instance.ExternalRef,
                        transferData.Amount,
                        DateTime.UtcNow,
                        AccountType.CurrentAccount,
                        transferData.TransferId));

            }),
            _ => throw new InvalidOperationException("Unsupported Transfer.")
        };

        await creditTask;
        transferData = transferData with { Status = DepositTransferStatus.BeneficiaryCredited };
        await _actorStateManager.SetStateAsync(DepositTransferState, transferData);
    }


    public async Task HandleCreditedEventAsync(string accountId)
    {
        var transferData = await _actorStateManager.GetStateAsync<DepositTransferData>(DepositTransferState);
        if (!accountId.Equals(transferData.BeneficiaryAccountId, StringComparison.Ordinal) ||
            transferData.Status != DepositTransferStatus.BeneficiaryCredited)
        {
            return;
        }
        transferData = transferData with { Status = DepositTransferStatus.Completed };
        await _actorStateManager.SetStateAsync(DepositTransferState, transferData);
    }



    private async Task<string?> StartTransferCurrentToSavings(DepositTransferData transferData)
    {
        var registerReminder = false;
        var currentAccount = (await _currentAccountQueryHandler.GetAccountAsync(transferData.DebtorAccountId))
            ?? throw new InvalidOperationException($"Cannot find an account by {nameof(transferData.DebtorAccountId)}.");

        if (currentAccount.TotalBalance < transferData.Amount)
        {
            registerReminder = true;
        }
        else
        {
            await _eventPublishingService.PublishCommand(
                new DebitAccountCommand(
                        $"{transferData.TransferId}-Debit",
                    currentAccount.ExternalRef,
                    transferData.Amount,
                    DateTime.UtcNow,
                    AccountType.CurrentAccount,
                    transferData.TransferId));

            transferData = transferData with { Status = DepositTransferStatus.DebtorDebited };

            await _actorStateManager.SetStateAsync(DepositTransferState, transferData);
            if (!transferData.IsFirstAttempt)
            {
                return TransferAttemptUnregister;
            }
        }

        if (transferData.IsFirstAttempt && registerReminder)
        {
            transferData = transferData with { IsFirstAttempt = false };

            await _actorStateManager.SetStateAsync(DepositTransferState, transferData);

            return TransferAttemptRegister;
        }

        return null;
    }

    private async Task<string?> StartTransferSavingsToCurrent(DepositTransferData transferData)
    {
        var savingsAccount = await _iasaQueryHandler.GetAccountAsync(transferData.DebtorAccountId)
            ?? throw new InvalidOperationException($"Cannot find an account by {nameof(transferData.DebtorAccountId)}.");

        if (savingsAccount.TotalBalance < transferData.Amount)
        {
            throw new InvalidOperationException($"Cannot withdraw more than savings account balance");
        }
        else
        {
            await _eventPublishingService.PublishCommand(
                new DebitAccountCommand(
                    $"{transferData.TransferId}-Debit",
                    savingsAccount.ExternalRef!,
                    transferData.Amount,
                    DateTime.UtcNow,
                    AccountType.SavingsAccount,
                    transferData.TransferId));

            transferData = transferData with { Status = DepositTransferStatus.DebtorDebited };
            await _actorStateManager.SetStateAsync(DepositTransferState, transferData);
        }

        return null;
    }
}
