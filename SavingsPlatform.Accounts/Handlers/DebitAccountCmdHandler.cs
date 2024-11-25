using MediatR;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Aggregates.InstantAccess;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Commands;
using SavingsPlatform.Common.Helpers;
using SavingsPlatform.Accounts.Current;
using SavingsPlatform.Accounts.Current.Models;
using SavingsPlatform.Contracts.Accounts.Enums;
using Microsoft.Extensions.Logging;

namespace SavingsPlatform.Accounts.Handlers;

public class DebitAccountCmdHandler : IRequestHandler<DebitAccountCommand>
{
    private readonly IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState> _savingsAccountFactory;
    private readonly IAggregateRootFactory<CurrentAccount, CurrentAccountState> _currentAccountFactory;
    private readonly IThreadSynchronizer _threadSynchronizer;
    private readonly ILogger<DebitAccountCmdHandler> _logger;
    public DebitAccountCmdHandler(
        IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState> aggregateFactory,
        IAggregateRootFactory<CurrentAccount, CurrentAccountState> currentAccountFactory,
        IThreadSynchronizer threadSynchronizer,
        ILogger<DebitAccountCmdHandler> logger)
    {
        _savingsAccountFactory = aggregateFactory;
        _currentAccountFactory = currentAccountFactory;
        _threadSynchronizer = threadSynchronizer;
        _logger = logger;
    }

    public async Task Handle(DebitAccountCommand request, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            throw new TaskCanceledException();
        }

        await _threadSynchronizer.ExecuteSynchronizedAsync(request.ExternalRef, () =>
        {
            _logger.LogInformation("Sync'ed processing of {CmdName} cmd for {ExternalRef} with CmdId = {MsgId}.",
                nameof(DebitAccountCommand),
                request.ExternalRef,
                request.MsgId);

            return request.Type switch
            {
                AccountType.CurrentAccount => Task.Run(async () =>
                {
                    _logger.LogInformation("Sync'ed processing of {CmdName} cmd for {ExternalRef} with CmdId = {MsgId}.",
                        nameof(DebitAccountCommand),
                        request.ExternalRef,
                        request.MsgId);
                    var instance = await _currentAccountFactory.GetInstanceByExternalRefAsync(request.ExternalRef);
                    await instance.DebitAsync(request);
                }),
                AccountType.SavingsAccount => Task.Run(async () =>
                {
                    _logger.LogInformation("Sync'ed processing of {CmdName} cmd for {ExternalRef} with CmdId = {MsgId}.",
                        nameof(DebitAccountCommand),
                        request.ExternalRef,
                        request.MsgId);
                    var instance = await _savingsAccountFactory.GetInstanceByExternalRefAsync(request.ExternalRef);
                    await instance.DebitAsync(request);
                })
            };
        });
    }
}
