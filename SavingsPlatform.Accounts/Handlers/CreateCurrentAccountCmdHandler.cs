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

namespace SavingsPlatform.Accounts.Handlers;

public class CreateCurrentAccountCmdHandler : IRequestHandler<CreateCurrentAccountCommand>
{
    private readonly IAggregateRootFactory<CurrentAccount, CurrentAccountState> _aggregateFactory;
    private readonly IThreadSynchronizer _threadSynchronizer;

    public CreateCurrentAccountCmdHandler(
        IAggregateRootFactory<CurrentAccount, CurrentAccountState> aggregateFactory,
        IThreadSynchronizer threadSynchronizer)
    {
        _aggregateFactory = aggregateFactory;
        _threadSynchronizer = threadSynchronizer;
    }

    public async Task Handle(CreateCurrentAccountCommand request, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
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