using Marten;
using SavingsOnDapr.EventStore.Aggregations;
using SavingsPlatform.Contracts.Accounts.Models;

namespace SavingsOnDapr.EventStore.Store;

public class CurrencyExchangeEventStore(IDocumentStore documentStore) : EventStoreBase(documentStore)
{
    public async Task<CurrencyExchangeSummaryDto?> GetSummary(string streamId)
    {
        using var session = _documentStore.LightweightSession();

        var result = await session.Events.AggregateStreamAsync(streamId, 0, null, new CurrencyExchangeSummary());

        if (result is not null)
        {
            return result.MapToDto();
        }

        return null;
    }

}
