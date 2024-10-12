using MediatR;
using SavingsPlatform.Accounts.Aggregates.InstantAccess;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Common.Helpers;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Commands;

namespace SavingsPlatform.Accounts.Handlers;

public class AccrueInterestCmdHandler(
    IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState> aggregateFactory,
    IThreadSynchronizer threadSynchronizer) : IRequestHandler<AccrueInterestCommand>
{
    private readonly IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState> _savingsAccountFactory = aggregateFactory;
    private readonly IThreadSynchronizer _threadSynchronizer = threadSynchronizer;

    public Task Handle(AccrueInterestCommand request, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            throw new TaskCanceledException();
        }

        return _threadSynchronizer.ExecuteSynchronizedAsync(request.ExternalRef, async () =>
        {
            var instance = await _savingsAccountFactory.GetInstanceAsync(request.AccountId);
            await instance.AccrueInterest(request.AccrualFrom, request.AccrualDate, request.AdjustedBalance);
        });

    }
}