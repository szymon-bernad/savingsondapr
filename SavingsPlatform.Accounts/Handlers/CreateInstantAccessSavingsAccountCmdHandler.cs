using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Aggregates.InstantAccess;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Commands;
using MediatR;
using SavingsPlatform.Common.Helpers;
using Dapr.Actors;
using SavingsPlatform.Accounts.Actors;
using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Models;
using Dapr.Actors.Client;

namespace SavingsPlatform.Accounts.Handlers;

public class CreateInstantAccessSavingsAccountCmdHandler : IRequestHandler<CreateInstantSavingsAccountCommand>
{
    private readonly IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState> _aggregateFactory;
    private readonly IActorProxyFactory _actorProxyFactory;

    public CreateInstantAccessSavingsAccountCmdHandler(
        IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState> aggregateFactory,
        IActorProxyFactory actorProxyFactory)
    {
        _aggregateFactory = aggregateFactory;
        _actorProxyFactory = actorProxyFactory;
    }

    public async Task Handle(CreateInstantSavingsAccountCommand request, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            throw new TaskCanceledException();
        }

        if (string.IsNullOrEmpty(request.ExternalRef))
        {
            throw new InvalidOperationException($"{nameof(request.ExternalRef)} cannot be null or empty.");
        }

        var actorInstance = _actorProxyFactory.CreateActorProxy<IAccountCreationActor>(
            new ActorId(request.ExternalRef),
            nameof(AccountCreationActor));

        await actorInstance.InitiateAsync(
            new AccountCreationData
            {
                ExternalRef = request.ExternalRef,
                AccountType = AccountType.SavingsAccount,
                Currency = request.AccountCurrency,
                CreatedAt = DateTime.UtcNow,
                UserId = request.UserId,
                CurrentAccountId = request.CurrentAccountId,
                InterestRate = request.InterestRate,
            });

    }
}
