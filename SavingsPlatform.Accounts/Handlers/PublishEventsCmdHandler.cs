using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Aggregates.InstantAccess;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Commands;
using SavingsPlatform.Contracts.Accounts.Enums;
using MediatR;
using SavingsPlatform.Common.Helpers;

namespace SavingsPlatform.Accounts.Handlers;

public class PublishEventsCommandHandler : IRequestHandler<PublishEventsCommand>
{
    private readonly IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState> _instantAccessFactory;
    private readonly IThreadSynchronizer _threadSynchronizer;


    public PublishEventsCommandHandler(
        IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState> iasaFactory,
        IThreadSynchronizer threadSynchronizer)
    {
        _instantAccessFactory = iasaFactory;
        _threadSynchronizer = threadSynchronizer;
    }

    public async Task Handle(PublishEventsCommand request, CancellationToken cancellationToken)
    {
        if (request.AccountType == AccountType.SavingsAccount)
        {
            await _threadSynchronizer.ExecuteSynchronizedAsync(request.AccountId, 
                async () =>
                {
                    var acc = await _instantAccessFactory.GetInstanceAsync(request.AccountId);
                    await acc.TryUpdateAsync(null);
                });
        }
    }
}