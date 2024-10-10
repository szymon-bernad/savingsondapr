using MediatR;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Aggregates.InstantAccess;
using SavingsPlatform.Accounts.Current.Models;
using SavingsPlatform.Accounts.Current;
using SavingsPlatform.Common.Helpers;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Commands;

namespace SavingsPlatform.Accounts.Handlers;

public class AccrueInterestCmdHandler : IRequestHandler<AccrueInterestCommand>
{
    private readonly IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState> _savingsAccountFactory;
    private readonly IAggregateRootFactory<CurrentAccount, CurrentAccountState> _currentAccountFactory;
    private readonly IThreadSynchronizer _threadSynchronizer;

    public AccrueInterestCmdHandler(
        IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState> aggregateFactory,
        IThreadSynchronizer threadSynchronizer)
    {
        _savingsAccountFactory = aggregateFactory;
        _threadSynchronizer = threadSynchronizer;
    }

    public async Task Handle(AccrueInterestCommand request, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            throw new TaskCanceledException();
        }

        var instance = await _savingsAccountFactory.GetInstanceAsync(request.AccountId);
        await instance.AccrueInterest(request.AccrualDate.AddDays(-1), request.AccrualDate, request.AdjustedBalance);
    }
}