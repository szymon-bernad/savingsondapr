using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Aggregates.InstantAccess;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Commands;
using MediatR;
using SavingsPlatform.Common.Helpers;

namespace SavingsPlatform.Accounts.Handlers;

public class CreateInstantAccessSavingsAccountCmdHandler : IRequestHandler<CreateInstantSavingsAccountCommand>
{
    private readonly IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState> _aggregateFactory;
    private readonly IThreadSynchronizer _threadSynchronizer;

    public CreateInstantAccessSavingsAccountCmdHandler(
        IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState> aggregateFactory,
        IThreadSynchronizer threadSynchronizer)
    {
        _aggregateFactory = aggregateFactory;
        _threadSynchronizer = threadSynchronizer;
    }

    public async Task Handle(CreateInstantSavingsAccountCommand request, CancellationToken cancellationToken)
    {
        if(cancellationToken.IsCancellationRequested)
        {
            throw new TaskCanceledException();
        }
        var instance = await _aggregateFactory.GetInstanceAsync();

        await _threadSynchronizer.ExecuteSynchronizedAsync(request.ExternalRef, async () =>
            {
                await instance.CreateAsync(request);
            });


    }
}
