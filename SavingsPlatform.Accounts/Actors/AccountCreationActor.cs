using Castle.Core.Logging;
using Dapr.Actors.Runtime;
using Microsoft.Extensions.Logging;
using SavingsPlatform.Accounts.AccountHolders;
using SavingsPlatform.Accounts.Actors.Services;
using SavingsPlatform.Accounts.Aggregates.InstantAccess;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Current;
using SavingsPlatform.Accounts.Current.Models;
using SavingsPlatform.Common.Helpers;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts;
using SavingsPlatform.Contracts.Accounts.Commands;
using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Models;
using static Google.Rpc.Context.AttributeContext.Types;

namespace SavingsPlatform.Accounts.Actors;

public class AccountCreationActor(
    ActorHost host,
    IAggregateRootFactory<CurrentAccount, CurrentAccountState> aggregateFactory,
    IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState> iasaAggregateFactory,
    IAggregateRootFactory<AccountHolder, AccountHolderState> accHolderFactory,
    ILogger<AccountCreationActor> logger) 
    : Actor(host), IAccountCreationActor
{
    private const string AccountCreationState = nameof(AccountCreationState);

    public async Task HandleAccountCreatedAsync(string accountId)
    {
        var actorData = await StateManager.GetStateAsync<AccountCreationData>(AccountCreationState);

        if (actorData is not null)
        {
            if (actorData.Status != AccountCreationStatus.CreatedUnassigned)
            {
                throw new InvalidOperationException(
                    $"{nameof(AccountCreationActor.HandleAccountCreatedAsync)} called" +
                    $" with Actor Id = {Id} having invalid status: {actorData.Status}");
            }

            var accHolder = await accHolderFactory.GetInstanceAsync(actorData.UserId);
            if (accHolder is null)
            {
                throw new InvalidOperationException(
                    $"AccountHolder with UserId = {actorData.UserId} not found.");
            }

            await accHolder.AddAccounts([new AccountInfo(accountId, AccountType.CurrentAccount)]);

            actorData = actorData with { Status = AccountCreationStatus.Completed, AccountId = accountId };
            await StateManager.SetStateAsync(AccountCreationState, actorData);
        }
    }

    public async Task InitiateAsync(AccountCreationData data)
    {
        try
        {
            var actorData = await StateManager.GetStateAsync<AccountCreationData>(AccountCreationState);

            if (actorData is not null)
            {
                if (actorData.Status != AccountCreationStatus.New)
                {
                    return;
                }
            }
        }
        catch (KeyNotFoundException kex)
        {
            logger.LogWarning(
                kex,
                $"State {AccountCreationState} not found for Actor Id = {Id}. " +
                "This is expected if the actor is being created for the first time.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error retrieving state {AccountCreationState} for Actor Id = {Id}.");
            throw;
        }

        if (data.AccountType == AccountType.CurrentAccount)
        {
            await InitiateCurrentAccount(data);
        }
        else if (data.AccountType == AccountType.SavingsAccount)
        {
            await InitiateInstantAccessSavingsAccount(data);
        }
        else
        {
            throw new InvalidOperationException($"Unsupported account type: {data.AccountType}");
        }

        data = data with { Status = AccountCreationStatus.CreatedUnassigned };
        await StateManager.SetStateAsync(AccountCreationState, data);
    }

    private async Task InitiateCurrentAccount(AccountCreationData data)
    {
        var instance = await aggregateFactory.TryGetInstanceByExternalRefAsync(data.ExternalRef);
        if (instance is not null)
        {
            data = data with { Status = AccountCreationStatus.FailedDuplicateExternalRef };
            await StateManager.SetStateAsync(AccountCreationState, data);

            throw new InvalidOperationException(
                $"CurrentAccount with {nameof(CurrentAccountState.ExternalRef)} = {data.ExternalRef} already exists.");
        }

        instance = await aggregateFactory.GetInstanceAsync();
        await instance.CreateAsync(
            new CreateCurrentAccountCommand(
                Guid.Empty.ToString(),
                data.ExternalRef,
                data.UserId,
                data.Currency));
    }

    private async Task InitiateInstantAccessSavingsAccount(AccountCreationData data)
    {
        var instance = await iasaAggregateFactory.TryGetInstanceByExternalRefAsync(data.ExternalRef);
        if (instance is not null)
        {
            data = data with { Status = AccountCreationStatus.FailedDuplicateExternalRef };
            await StateManager.SetStateAsync(AccountCreationState, data);
            throw new InvalidOperationException(
                $"InstantAccessSavingsAccount with {nameof(InstantAccessSavingsAccountState.ExternalRef)} = {data.ExternalRef} already exists.");
        }
        instance = await iasaAggregateFactory.GetInstanceAsync();
        await instance.CreateAsync(
            new CreateInstantSavingsAccountCommand(
                Guid.Empty.ToString(),
                data.ExternalRef,
                data.InterestRate,
                data.CurrentAccountId,
                data.UserId,
                data.Currency));
    }
}
