using Marten;
using SavingsPlatform.Contracts.Accounts.Models;
using ISavingsEvent = SavingsPlatform.Contracts.Accounts.Interfaces.IEvent;
using IMartenEvent = Marten.Events.IEvent;
using System.Collections;
using SavingsOnDapr.EventStore.Aggregations;

namespace SavingsOnDapr.EventStore.Store;

public class AccountHierarchyEventStore(IDocumentStore documentStore) : EventStoreBase(documentStore)
{

    public async Task<AccountHierarchySummaryDto?> GetAccountHierarchySummary(string streamId, DateTime? fromDate, DateTime? toDate)
    {
        using var session = _documentStore.LightweightSession();

        var summary = await session.Events.AggregateStreamAsync(streamId, 0, toDate, new AccountHierarchySummary(fromDate, toDate));

        if (summary is not null)
        {
            return new AccountHierarchySummaryDto(
                fromDate, toDate, streamId, 
                summary.TotalAmountTransferredToSavings,
                summary.TotalAmountWithdrawnFromSavings,
                summary.TotalAmountOfNewDeposits,
                summary.TotalAmountOfWithdrawals,
                summary.TotalCountOfDepositTransfers);
        }

        return null;
    }

    public async Task<AccountHierarchyBalanceRangesDto?> GetAccountHierarchyBalances(string streamId, DateTime? fromDate, DateTime? toDate)
    {
        using var session = _documentStore.LightweightSession();

        var balances = await session.Events.AggregateStreamAsync(streamId, 0, toDate, new AccountHierarchyBalanceSummary(fromDate, toDate));

        if (balances is not null)
        {
            return balances.MapToDto();
        }

        return null;
    }
}
