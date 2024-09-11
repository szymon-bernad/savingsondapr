using MediatR;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Aggregates.InstantAccess;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Commands;
using SavingsPlatform.Common.Helpers;

namespace SavingsPlatform.Accounts.Handlers;

public class CreditAccountCmdHandler : IRequestHandler<CreditAccountCommand>
{
    private readonly IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState> _aggregateFactory;
    private readonly IThreadSynchronizer _threadSynchronizer;

    public CreditAccountCmdHandler(
        IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState> aggregateFactory,
        IThreadSynchronizer threadSynchronizer)
    {
        _aggregateFactory = aggregateFactory;
        _threadSynchronizer = threadSynchronizer;
    }

    public async Task Handle(CreditAccountCommand request, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            throw new TaskCanceledException();
        }

        await _threadSynchronizer.ExecuteSynchronizedAsync(request.ExternalRef, 
            async () =>
            {
                var instance = await _aggregateFactory.GetInstanceByExternalRefAsync(request.ExternalRef);
                await instance.CreditAsync(request);
            });

    }
}
