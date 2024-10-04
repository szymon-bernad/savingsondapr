using Marten;
using Marten.Events;
using SavingsPlatform.Contracts.Accounts.Interfaces;
using SavingsPlatform.Contracts.Accounts.Models;

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

        var processedEventsTasks = events.Select((e) => session.LoadAsync<EventStatusEntry>(e.Id));
        var processedEvents = (await Task.WhenAll(processedEventsTasks)).Where(r => r is not null).Select(e => e!.EventId);

        var newEvents = events.ExceptBy(processedEvents, e => e.Id);

        if (newEvents.Any())
        {
            var streamState = await session.Events.FetchStreamStateAsync(streamId, cancellationToken);
            if (streamState is null)
            {
                session.Events.StartStream(streamId, newEvents);
            }
            else
            {
                await session.Events.AppendExclusive(streamId);
                session.Events.Append(streamId, streamState.Version + newEvents.Count(), newEvents);
            }

            session.Store<EventStatusEntry>(newEvents.Select(e => new EventStatusEntry { EventId = e.Id }));

            await session.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IEnumerable<Marten.Events.IEvent>> FetchEventsAsync(string streamId)
    {
        using var session = _documentStore.LightweightSession();

        var events = await session.Events.FetchStreamAsync(streamId);

        return events;
    }
}
