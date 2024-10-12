using MediatR;
using SavingsPlatform.Accounts.ApiClients;
using SavingsPlatform.Contracts.Accounts.Commands;

namespace SavingsPlatform.Accounts.Handlers;

public class AccrueInterestForAccountsCmdHandler(
    IEventStoreApiClient eventStoreApiClient,
    IMediator mediator) 
        : IRequestHandler<AccrueInterestForAccountsCommand>
{
    private readonly IEventStoreApiClient _eventStoreApiClient = eventStoreApiClient;
    private readonly IMediator _mediator = mediator;

    public async Task Handle(AccrueInterestForAccountsCommand request, CancellationToken cancellationToken)
    {
        var accrualFrom = request.AccrualDate.AddDays(-1);
        var balances = await _eventStoreApiClient.GetBalancesForAccountHierarchyAsync(
            request.CurrentAccountId, 
            accrualFrom,
            request.AccrualDate);

        await Task.WhenAll(request.SavingsAccountIds.Select(acc =>
            _mediator.Send(new AccrueInterestCommand(
                Guid.NewGuid().ToString(),
                acc.AccountId,
                acc.ExternalRef,
                request.AccrualDate,
                acc.AccrualFrom ?? accrualFrom,
                (balances?.ContainsKey(acc.AccountId) ?? false) ? 
                    (balances[acc.AccountId].MinTotalBalance + balances[acc.AccountId].MaxTotalBalance)*0.5m :
                    null))
            ));

    }
}
