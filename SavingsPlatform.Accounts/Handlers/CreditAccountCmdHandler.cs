using MediatR;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Aggregates.InstantAccess;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Commands;
using SavingsPlatform.Common.Helpers;
using SavingsPlatform.Accounts.Current;
using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Accounts.Current.Models;
using Microsoft.Extensions.Logging;

namespace SavingsPlatform.Accounts.Handlers;

public class CreditAccountCmdHandler : IRequestHandler<CreditAccountCommand>
{
    private readonly IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState> _savingsAccountFactory;
    private readonly IAggregateRootFactory<CurrentAccount, CurrentAccountState> _currentAccountFactory;
    private readonly IThreadSynchronizer _threadSynchronizer;
    private readonly ILogger<CreditAccountCmdHandler> _logger;

    public CreditAccountCmdHandler(
        IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState> aggregateFactory,
        IAggregateRootFactory<CurrentAccount, CurrentAccountState> currentAccountFactory,
        IThreadSynchronizer threadSynchronizer,
        ILogger<CreditAccountCmdHandler> logger)
    {
        _savingsAccountFactory = aggregateFactory;
        _currentAccountFactory = currentAccountFactory;
        _threadSynchronizer = threadSynchronizer;
        _logger = logger;
    }

    public async Task Handle(CreditAccountCommand request, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            throw new TaskCanceledException();
        }

        await _threadSynchronizer.ExecuteSynchronizedAsync(request.ExternalRef, () =>
        {
            return request.Type switch
            {
                AccountType.CurrentAccount => Task.Run(async () =>
                {
                    _logger.LogInformation("Sync'ed processing of {CmdName} cmd for {ExternalRef} with CmdId = {MsgId}.",
                        nameof(CreditAccountCommand),
                        request.ExternalRef,
                        request.MsgId);

                    var instance = await _currentAccountFactory.GetInstanceByExternalRefAsync(request.ExternalRef);
                    await instance.CreditAsync(request);
                }),

                AccountType.SavingsAccount => Task.Run(async () =>
                {
                    _logger.LogInformation("Sync'ed processing of {CmdName} cmd for {ExternalRef} with CmdId = {MsgId}.",
                        nameof(CreditAccountCommand),
                        request.ExternalRef,
                        request.MsgId);
                    var instance = await _savingsAccountFactory.GetInstanceByExternalRefAsync(request.ExternalRef);
                    await instance.CreditAsync(request);
                }),
                _ => throw new InvalidOperationException("Invalid account type"),
            };
        });

    }
}
