using Carter;
using Dapr;
using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Client;
using MediatR;
using Microsoft.Extensions.Options;
using SavingsPlatform.Accounts.Actors;
using SavingsPlatform.Accounts.Aggregates.InstantAccess;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Current.Models;
using SavingsPlatform.Common.Config;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Common.Services;
using SavingsPlatform.Contracts.Accounts.Commands;
using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Events;
using System.Text.Json;
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
                    nameof(DepositTransferActor),
                    new ActorProxyOptions { });
                await actorInstance.HandleDebitedEventAsync(@event.AccountId);
            }
        }).WithTags(["platform"]); ;

        app.MapPost("v1/accounts/:handle-credited-event",
                    [Topic("pubsub", "accountcredited")] 
                    async (AccountCredited @event, IActorProxyFactory actorProxyFactory) =>
        {
            if (@event.TransferId != null)
            {
                var actorInstance = actorProxyFactory.CreateActorProxy<IDepositTransferActor>(
                    new ActorId(@event.TransferId),
                    nameof(DepositTransferActor));

                await actorInstance.HandleCreditedEventAsync(@event.AccountId);
            }
        }).WithTags(["platform"]);


        app.MapPost("/api/platform/commands",
                    [Topic("pubsub", "commands")] 
                    async (PubSubCommand evt, IMediator mediator, ILogger<PlatformModule> logger) =>
        {
            try
            {
                if (evt is not null && evt.Data is not null)
                {
                    var cmdString = JsonSerializer.Serialize(evt.Data);
                    var type = Type.GetType(evt.CommandType, true);

                    if type is DummyCommand.GetType()
                    {
                        logger.LogInformation($"Received dummy command with [Id = {evt.MsgId}]");
                        return Results.NoContent();
                    }

                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    jsonOptions.Converters.Add(new JsonStringEnumConverter());

                    var cmd = JsonSerializer.Deserialize(cmdString, type!, jsonOptions);
                    logger.LogInformation($"Received command with [Id = {evt.MsgId}]");
                    await mediator.Send(cmd!);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error processing command with [Id = {evt.MsgId}]: {ex.Message}");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
            return Results.NoContent();
        }).WithTags(["platform"]);

        app.MapPost("/api/platform/publish-events",
                    async (IStateEntryQueryHandler<InstantAccessSavingsAccountState> iasaRepository,
                    IMediator mediator) =>
        {
            var iasaRes = await iasaRepository.QueryAccountsByKeyAsync(["hasUnpublishedEvents"], [true]);

            await Task.WhenAll(
                            iasaRes.Select(
                                acc => mediator.Send(new PublishEventsCommand(acc.Key, AccountType.SavingsAccount))
                                ));

            return Results.Ok();
        }).WithTags(["platform"]);

        app.MapMethods("/api/platform/publish-events", 
                      ["OPTIONS"], () => Task.FromResult(Results.Accepted())).WithTags(["platform"]);

        app.MapGet("/api/platform/savings-account/command/{msgid}", 
        async (string msgid, IStateEntryRepository<InstantAccessSavingsAccountState> repo) =>
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

        app.MapGet("/api/platform/accounts/command/{msgid}",
            async (string msgid, IStateEntryRepository<CurrentAccountState> repo) =>
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

        app.MapPost("/api/platform/accrue-interest",
        async (IStateEntryQueryHandler<InstantAccessSavingsAccountState> iasaRepository,
               IEventPublishingService publishingService) =>
        {
           var dtActivated = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ss");
            var dtAccrued = DateTime.UtcNow.AddHours(-20).ToString("yyyy-MM-ddTHH:mm:ss");
            var iasaRes = await iasaRepository.QueryAccountsByKeyAsync(
                    ["data.activatedOn LessThan", "data.interestAccruedOn LessThanOrEqual"], 
                    [dtActivated, dtAccrued]);

            if (iasaRes.Count != 0)
            {
                var grouped = iasaRes.GroupBy(acc => acc.CurrentAccountId);
                
                await Task.WhenAll(
                    grouped.Select(g =>
                        {          
                            var cmdId = Guid.NewGuid().ToString();
                            var accountEntries = g.Select(acc => new AccountAccrualEntry(acc.Key, acc.ExternalRef, acc.InterestAccruedOn));

                            var transferCmd = new AccrueInterestForAccountsCommand(cmdId, g.Key, accountEntries, DateTime.UtcNow);
                            return publishingService.PublishCommand(transferCmd);
                        }));
            }

            return Results.Ok();
        }).WithTags(["platform"]);

        app.MapPost("/api/platfrom/migrate-savings",
            async(IStateEntryQueryHandler<InstantAccessSavingsAccountState> iasaRepository,
                 IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState> iasaFactory) =>
            { 
                var accounts = await iasaRepository.QueryAccountsByKeyAsync([ "data.type" ], ["SavingsAccount"]);

                foreach(var acc in accounts)
                {
                    var aggr = await iasaFactory.GetInstanceAsync(acc.Key);
                    await aggr.DummyUpdateAsync();
                }
                return Results.Ok();
            }).WithTags(["platform"]);

        app.MapMethods("/api/platform/accrue-interest", ["OPTIONS"],
            () => Task.FromResult(Results.Accepted())).WithTags(["platform"]);

        app.MapGet("/healthz",
            async (DaprClient client,
                    IStateEntryQueryHandler<InstantAccessSavingsAccountState> iasaRepository) =>
                {
                    try
                    {
                        var healthy = await client.CheckHealthAsync();
                        var dbCheck = await iasaRepository.QueryAccountsByKeyAsync(["data.type"], ["SavingsAccount"]);

                        if (healthy)
                        {
                            await publishingService.PublishCommand(
                                new DummyCommand(Guid.NewGuid());

                            return Results.Ok();
                        }
                        else
                        {
                            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
                        }
                    }
                    catch (Exception ex)
                    {
                        return Results.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
                    }
                }).WithTags(["platform"]);

        app.MapGet("/healthver", (IOptions<ServiceConfig> cfg) => Results.Ok(new { Version = cfg?.Value?.Version ?? "std" })).WithTags(["platform"]);

    }
}
