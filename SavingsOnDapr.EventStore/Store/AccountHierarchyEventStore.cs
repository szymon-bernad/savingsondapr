using Marten;
using SavingsPlatform.Contracts.Accounts.Models;
using ISavingsEvent = SavingsPlatform.Contracts.Accounts.Interfaces.IEvent;
using IMartenEvent = Marten.Events.IEvent;
using System.Collections;

namespace SavingsOnDapr.EventStore.Store;

public class AccountHierarchyEventStore(IDocumentStore documentStore)
{
    private readonly IDocumentStore _documentStore = documentStore;

    public async Task AppendEventsAsync(
        string streamId,
        IEnumerable<ISavingsEvent> events,
        CancellationToken cancellationToken)
    {
        using var session = _documentStore.LightweightSession();

        var newEvents = await this.DeduplicateEvents(session, events, cancellationToken);

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

    public async Task<IEnumerable<IMartenEvent>> FetchEventsAsync(string streamId)
    {
        using var session = _documentStore.LightweightSession();

        var events = await session.Events.FetchStreamAsync(streamId);

        return events;
    }

    public async Task<AccountHierarchySummaryDto?> GetAccountHierarchySummary(string streamId, DateTime? fromDate, DateTime? toDate)
    {
        using var session = _documentStore.LightweightSession();

        var summary = await session.Events.AggregateStreamAsync(streamId, 0, null, new AccountHierarchySummary(fromDate, toDate));

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

    private async Task<IEnumerable<ISavingsEvent>> DeduplicateEvents(
        IDocumentSession session,
        IEnumerable<ISavingsEvent> events,
        CancellationToken cancellationToken)
    {
        var processedEventsTasks = events.Select((e) => session.LoadAsync<EventStatusEntry>(e.Id));
        var processedEvents = (await Task.WhenAll(processedEventsTasks)).Where(r => r is not null).Select(e => e!.EventId);

        return events.ExceptBy(processedEvents, e => e.Id);
    }
}
