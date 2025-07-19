using MediatR;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Aggregates.InstantAccess;
using SavingsPlatform.Common.Helpers;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SavingsPlatform.Accounts.Current.Models;
using SavingsPlatform.Accounts.Current;
using Dapr.Actors.Client;
using Dapr.Actors;
using Marten.Events;
using SavingsPlatform.Accounts.Actors;
using SavingsPlatform.Contracts.Accounts.Models;
using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Accounts.Handlers;

public class CreateCurrentAccountCmdHandler : IRequestHandler<CreateCurrentAccountCommand>
{
    private readonly IAggregateRootFactory<CurrentAccount, CurrentAccountState> _aggregateFactory;
    private readonly IThreadSynchronizer _threadSynchronizer;
    private readonly IActorProxyFactory _actorProxyFactory;

    public CreateCurrentAccountCmdHandler(
        IAggregateRootFactory<CurrentAccount, CurrentAccountState> aggregateFactory,
        IThreadSynchronizer threadSynchronizer,
        IActorProxyFactory actorProxyFactory)
    {
        _aggregateFactory = aggregateFactory;
        _threadSynchronizer = threadSynchronizer;
        this._actorProxyFactory = actorProxyFactory;
    }

    public async Task Handle(CreateCurrentAccountCommand request, CancellationToken cancellationToken)
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
                AccountType = AccountType.CurrentAccount,
                Currency = request.AccountCurrency,
                CreatedAt = DateTime.UtcNow,
                UserId = request.UserId,
            });

    }
}