namespace SavingsOnDapr.EventStore.Store;

using Marten;
using SavingsPlatform.Contracts.Accounts.Models;
using ISavingsEvent = SavingsPlatform.Contracts.Accounts.Interfaces.IEvent;
using IMartenEvent = Marten.Events.IEvent;

public abstract class EventStoreBase(IDocumentStore documentStore)
{
    protected readonly IDocumentStore _documentStore = documentStore;
    public async Task AppendEventsAsync(
    string streamId,
    IEnumerable<ISavingsEvent> events,
    CancellationToken cancellationToken)
    {
        using var session = _documentStore.LightweightSession();

        var newEvents = await DeduplicateEvents(session, events, cancellationToken);

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

            session.Store(newEvents.Select(e => new EventStatusEntry { EventId = e.Id }));

            await session.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IEnumerable<IMartenEvent>> FetchEventsAsync(string streamId)
    {
        using var session = _documentStore.LightweightSession();

        var events = await session.Events.FetchStreamAsync(streamId);

        return events;
    }

    protected static async Task<IEnumerable<ISavingsEvent>> DeduplicateEvents(
        IDocumentSession session,
        IEnumerable<ISavingsEvent> events,
        CancellationToken cancellationToken)
    {
        var processedEventsTasks = events.Select((e) => session.LoadAsync<EventStatusEntry>(e.Id));
        var processedEvents = (await Task.WhenAll(processedEventsTasks)).Where(r => r is not null).Select(e => e!.EventId);

        return events.ExceptBy(processedEvents, e => e.Id);
    }
}
