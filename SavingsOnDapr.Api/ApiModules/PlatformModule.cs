using Carter;
using Dapr;
using Dapr.Actors;
using Dapr.Actors.Client;
using MediatR;
using SavingsPlatform.Accounts.Actors;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Commands;
using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Events;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace SavingsOnDapr.Api.ApiModules;

public class PlatformModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/platform/accounts/:handle-iasaactivated-event",
            [Topic("pubsub", "instantaccesssavingsaccountactivated")] (InstantAccessSavingsAccountActivated @event, ILogger<PlatformModule> logger) =>
            {
                logger.LogInformation("Received IASA activated event: {accountId}", @event.AccountId);
            }).WithTags(["platform"]);


        app.MapPost("v1/accounts/:handle-debited-event",
                    [Topic("pubsub", "accountdebited")]
                     async (AccountDebited @event,
                            IActorProxyFactory actorProxyFactory,
                            ILogger<PlatformModule> logger) =>
                    {

                        logger.LogInformation($"Handling debited event [AccountRef = {@event}, "    +
                            $"Amount = {@event.Amount}, TransferId = {@event.TransferId}]");
                        if (@event.TransferId != null)
                        {
                            var actorInstance = actorProxyFactory.CreateActorProxy<IDepositTransferActor>(
                                new ActorId(@event.TransferId),
                                nameof(DepositTransferActor));
                       
                            await actorInstance.HandleDebitedEventAsync(@event.AccountId);
                        }
                    }).WithTags(["platform"]); ;

        app.MapPost("v1/accounts/:handle-credited-event",
                    [Topic("pubsub", "accountcredited")] async (AccountCredited @event, IActorProxyFactory actorProxyFactory) =>
                    {
                        if (@event.TransferId != null)
                        {
                            var actorInstance = actorProxyFactory.CreateActorProxy<IDepositTransferActor>(
                                new ActorId(@event.TransferId),
                                nameof(DepositTransferActor));

                            await actorInstance.HandleCreditedEventAsync(@event.AccountId);
                        }
                    }).WithTags(["platform"]); ;


        app.MapPost("/api/platform/commands",
                    [Topic("pubsub", "commands")] async (PubSubCommand evt, IMediator mediator, ILogger<PlatformModule> logger) =>
                    {
                        try
                        {
                            if (evt is not null && evt.Data is not null)
                            {
                                var cmdString = JsonSerializer.Serialize(evt.Data);
                                var type = Type.GetType(evt.CommandType, true);

                                var jsonOptions = new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true
                                };
                                jsonOptions.Converters.Add(new JsonStringEnumConverter());

                                var cmd = JsonSerializer.Deserialize(cmdString, type!, jsonOptions);
                                logger.LogInformation($"Received command with Id = {evt.MsgId}");
                                await mediator.Send(cmd!);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError($"Error processing command: {ex.Message}");
                            throw;
                        }
                    }).WithTags(["platform"]);

        app.MapPost("/api/platform/publish-events",
            async (
                   IStateEntryRepository<InstantAccessSavingsAccountState> iasaRepository,
                   IMediator mediator
                   ) =>
        {
            var iasaRes = await iasaRepository.QueryAccountsByKeyAsync(new string[] { "hasUnpublishedEvents" }, new string[] { "true" }, false);

            await Task.WhenAll(
                            iasaRes.Select(
                                acc => mediator.Send(new PublishEventsCommand(acc.Key, AccountType.SavingsAccount))
                                ));

            return Results.Ok();
        }).WithTags(["platform"]);

        app.MapMethods("/api/platform/publish-events", ["OPTIONS"],
            () => Task.FromResult(Results.Accepted())).WithTags(["platform"]);

        app.MapGet("/api/platform/savings-account/command/{msgid}", async (string msgid, IStateEntryRepository<InstantAccessSavingsAccountState> repo) =>
        {
            var result = (await repo.IsMessageProcessed(msgid));

            if (result)
            {
                return Results.Ok(new { MsgId = msgid, Status = "Processed" });
            }
            else
            {
                return Results.NotFound();
            }
        }).WithTags(["platform"]);

        app.MapGet("/healthz", () => Results.Ok()).WithTags(["platform"]);

    }
}
