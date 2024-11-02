using Carter;
using Dapr;
using Dapr.Actors.Client;
using Dapr.Client;
using Dapr.Workflow;
using SavingsPlatform.Contracts.Accounts.Events;

namespace CurrencyExchange.Api.ApiModules;

public class EventsModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("v1/events/:handle-debited-event",
             [Topic("pubsub", "accountdebited")]
        async (AccountDebited @event,
               DaprWorkflowClient daprClient,
               ILogger<EventsModule> logger) =>
             {
                 logger.LogInformation($"Handling debited event [AccountRef = {@event}, TransferId = {@event.TransferId}]");
                 if (@event.TransferId != null)
                 {
                     await daprClient.RaiseEventAsync(@event.TransferId, "accountdebited", @event);
                 }
             }).WithTags(["events"]);

        app.MapPost("v1/events/:handle-credited-event",
             [Topic("pubsub", "accountcredited")]
                async (AccountDebited @event,
               DaprWorkflowClient daprClient,
               ILogger<EventsModule> logger) =>
             {
                 logger.LogInformation($"Handling debited event [AccountRef = {@event}, TransferId = {@event.TransferId}]");
                 if (@event.TransferId != null)
                 {
                     await daprClient.RaiseEventAsync(@event.TransferId, "accountcredited", @event);
                 }
             }).WithTags(["events"]);
    }
}
