using Carter;
using Dapr;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SavingsOnDapr.EventStore.Store;
using SavingsPlatform.Contracts.Accounts.Events;

namespace SavingsOnDapr.EventStore.ApiModules;

public class EventsModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/events/:handle-created-event",
            [Topic("pubsub", "accountcreated")] async (
                AccountCreated @event,
                AccountHierarchyEventStore store) =>
            {

                await store.AppendEventsAsync(@event.CurrentAccountId, [@event], CancellationToken.None);

                return Results.Ok();
            });

        app.MapPost("v1/events/:handle-debited-event",
            [Topic("pubsub", "accountdebited")] async (
                AccountDebited @event,
                AccountHierarchyEventStore store) =>
            {
                await store.AppendEventsAsync(@event.CurrentAccountId, [@event], CancellationToken.None);

                return Results.Ok();
            });

        app.MapPost("v1/events/:handle-credited-event",
            [Topic("pubsub", "accountcredited")] async (
                AccountCredited @event,
                AccountHierarchyEventStore store) =>
            {
                await store.AppendEventsAsync(@event.CurrentAccountId, [@event], CancellationToken.None);

                return Results.Ok();
            });

        app.MapGet("v1/events/account/{id}",
            async (AccountHierarchyEventStore store, string id) =>
            {
                var events = await store.FetchEventsAsync(id);

                return Results.Ok(events.Select(e => e.Data));
            });

        app.MapGet("v1/events/accounts-summary/{id}",
            async (AccountHierarchyEventStore store, string id, [FromQuery]DateTime? fromDate, [FromQuery]DateTime? toDate) =>
            {
                var result = await store.GetAccountHierarchySummary(id, fromDate, toDate);

                return Results.Ok(result);
            });

        app.MapGet("v1/events/transactions/{id}",
            async (AccountHierarchyEventStore store, string id, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate) =>
            {
                var result = await store.GetAccountHierarchyTransactions(id, fromDate, toDate);

                return Results.Ok(result.Transactions);
            });

        app.MapGet("/healthz", () => Results.Ok());

    }
}
