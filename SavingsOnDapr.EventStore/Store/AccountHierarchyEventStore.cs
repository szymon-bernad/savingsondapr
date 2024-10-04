using Marten;
using Marten.Events;
using SavingsPlatform.Contracts.Accounts.Interfaces;

namespace SavingsOnDapr.EventStore.Store;

public class AccountHierarchyEventStore
{
    private readonly IDocumentStore _documentStore;
    public AccountHierarchyEventStore(IDocumentStore documentStore)
    {
        _documentStore = documentStore;
    }

    public async Task AppendEventsAsync(string streamId, IEnumerable<SavingsPlatform.Contracts.Accounts.Interfaces.IEvent> events, CancellationToken cancellationToken)
    {
        using var session = _documentStore.LightweightSession();

        var streamState = await session.Events.FetchStreamStateAsync(streamId);
        if (streamState is null)
        {
            session.Events.StartStream(streamId, events);
        }
        else
        {
            await session.Events.AppendExclusive(streamId);
            session.Events.Append(streamId, streamState.Version + events.Count(), events);
        }

        await session.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<Marten.Events.IEvent>> FetchEventsAsync(string streamId)
    {
        using var session = _documentStore.LightweightSession();

        var events = await session.Events.FetchStreamAsync(streamId);

        return events;
    }
}
