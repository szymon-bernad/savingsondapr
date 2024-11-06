using Carter;
using Dapr;
using Dapr.Actors.Client;
using Dapr.Client;
using Dapr.Workflow;
using Marten.Events;
using SavingsPlatform.Contracts.Accounts.Events;

namespace CurrencyExchange.Api.ApiModules;

public class EventsModule : ICarterModule
{
    private const char WorkflowOpSeparator = '^';

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("v1/events/:handle-debited-event",
             [Topic("pubsub", "accountdebited")]
             async (AccountDebited @event,
                    DaprWorkflowClient daprClient,
                    ILogger<EventsModule> logger) =>
             {
                 logger.LogInformation($"Handling debited event [AccountRef = {@event.ExternalRef}]");

                 var instanceId = ExtractInstanceId(@event.OperationId);
                 if(!string.IsNullOrEmpty(instanceId))
                 {
                     await daprClient.RaiseEventAsync(instanceId, "accountdebited", @event);
                 }

                 return Results.NoContent();

             }).WithTags(["events"]);

        app.MapPost("v1/events/:handle-credited-event",
             [Topic("pubsub", "accountcredited")]
             async (AccountCredited @event,
                    DaprWorkflowClient daprClient,
                    ILogger<EventsModule> logger) =>
             {
                logger.LogInformation($"Handling credited event [AccountRef = {@event.ExternalRef}]");

                var instanceId = ExtractInstanceId(@event.OperationId);
                if (!string.IsNullOrEmpty(instanceId))
                {
                    await daprClient.RaiseEventAsync(instanceId, "accountcredited", @event);
                }

                return Results.NoContent();
             }).WithTags(["events"]);
    }

    private static string? ExtractInstanceId(string? opId)
    {
        if (!string.IsNullOrEmpty(opId) && opId.Contains(WorkflowOpSeparator))
        {
            return opId[..opId.LastIndexOf(WorkflowOpSeparator)];
        }

        return null;
    }
}
