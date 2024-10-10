using MediatR;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Aggregates.InstantAccess;
using SavingsPlatform.Accounts.Current.Models;
using SavingsPlatform.Accounts.Current;
using SavingsPlatform.Common.Helpers;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Commands;
using SavingsPlatform.Accounts.ApiClients;

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
        var transactions = await _eventStoreApiClient.GetTransactionsForAccountHierarchyAsync(
            request.CurrentAccountId, 
            request.AccrualDate.AddDays(-1),
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

        await Task.WhenAll(request.SavingsAccountIds.Select(accountId =>
            _mediator.Send(new AccrueInterestCommand(
                Guid.NewGuid().ToString(),
                accountId,
                request.AccrualDate,
                balanceDictionary.ContainsKey(accountId) ? balanceDictionary[accountId] : null
            ))));

    }


}
