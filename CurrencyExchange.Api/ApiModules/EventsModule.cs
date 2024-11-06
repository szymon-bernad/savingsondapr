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
                 logger.LogInformation($"Handling debited event [AccountRef = {@event.ExternalRef}]");
                 if (!string.IsNullOrEmpty(@event.OperationId) && @event.OperationId.Contains('^'))
                {
                    var instanceId = @event.OperationId[..@event.OperationId.LastIndexOf('^')];
                    await daprClient.RaiseEventAsync(instanceId, "accountdebited", @event);
                }
             }).WithTags(["events"]);

        app.MapPost("v1/events/:handle-credited-event",
             [Topic("pubsub", "accountcredited")]
                async (AccountCredited @event,
               DaprWorkflowClient daprClient,
               ILogger<EventsModule> logger) =>
             {
                 logger.LogInformation($"Handling credited event [AccountRef = {@event.ExternalRef}]");
                 if (!string.IsNullOrEmpty(@event.OperationId) && @event.OperationId.Contains('^'))
                 {
                     var instanceId = @event.OperationId[..@event.OperationId.LastIndexOf('^')];
                     await daprClient.RaiseEventAsync(instanceId, "accountcredited", @event);
                 }
             }).WithTags(["events"]);
    }
}
