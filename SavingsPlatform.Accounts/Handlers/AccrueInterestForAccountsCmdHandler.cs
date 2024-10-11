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
        var transactions = await _eventStoreApiClient.GetTransactionsForAccountHierarchyAsync(
            request.CurrentAccountId, 
            accrualFrom,
            request.AccrualDate);

        var balanceDictionary = new Dictionary<string, decimal>();

        if (transactions.Any())
        {
            var groupedTxns = transactions.GroupBy(x => x.AccountId);

            foreach (var group in groupedTxns)
            {
                var balance = group.Average(x => x.TotalBalance);
                balanceDictionary.Add(group.Key, balance);
            }
        }

        await Task.WhenAll(request.SavingsAccountIds.Select(acc =>
            _mediator.Send(new AccrueInterestCommand(
                Guid.NewGuid().ToString(),
                acc.AccountId,
                acc.ExternalRef,
                request.AccrualDate,
                acc.AccrualFrom ?? accrualFrom,
                balanceDictionary.ContainsKey(acc.AccountId) ? balanceDictionary[acc.AccountId] : null
            ))));

    }


}
