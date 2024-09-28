using MediatR;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Aggregates.InstantAccess;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Commands;
using SavingsPlatform.Common.Helpers;
using SavingsPlatform.Accounts.Current;
using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Accounts.Current.Models;

namespace SavingsPlatform.Accounts.Handlers;

public class CreditAccountCmdHandler : IRequestHandler<CreditAccountCommand>
{
    private readonly IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState> _savingsAccountFactory;
    private readonly IAggregateRootFactory<CurrentAccount, CurrentAccountState> _currentAccountFactory;
    private readonly IThreadSynchronizer _threadSynchronizer;

    public CreditAccountCmdHandler(
        IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState> aggregateFactory,
        IAggregateRootFactory<CurrentAccount, CurrentAccountState> currentAccountFactory,
        IThreadSynchronizer threadSynchronizer)
    {
        _savingsAccountFactory = aggregateFactory;
        _currentAccountFactory = currentAccountFactory;
        _threadSynchronizer = threadSynchronizer;
    }

    public async Task Handle(CreditAccountCommand request, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            throw new TaskCanceledException();
        }

        await _threadSynchronizer.ExecuteSynchronizedAsync(request.ExternalRef, async () =>
        {
            var dbtTask = request.Type switch
            {
                AccountType.CurrentAccount => Task.Run(async () =>
                {
                    var instance = await _currentAccountFactory.GetInstanceByExternalRefAsync(request.ExternalRef);
                    return instance.CreditAsync(request);
                }),

                AccountType.SavingsAccount => Task.Run(async () =>
                {
                    var instance = await _savingsAccountFactory.GetInstanceByExternalRefAsync(request.ExternalRef);
                    return instance.CreditAsync(request);
                }),
                _ => throw new InvalidOperationException("Invalid account type"),
            };

            await dbtTask;
        });

    }
}
