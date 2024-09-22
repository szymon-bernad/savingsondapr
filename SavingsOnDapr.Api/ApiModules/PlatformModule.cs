using Carter;
using Dapr;
using MediatR;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Commands;
using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Events;
using System.Text.Json;

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

        app.MapPost("/api/platform/commands",
                    [Topic("pubsub", "commands")] async (PubSubCommand evt, IMediator mediator, ILogger<PlatformModule> logger) =>
                    {
                        try
                        {
                            if (evt is not null && evt.Data is not null)
                            {
                                var cmdString = JsonSerializer.Serialize(evt.Data);
                                var type = Type.GetType(evt.CommandType, true);
                                var cmd = JsonSerializer.Deserialize(cmdString, type!);
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

    }
}
