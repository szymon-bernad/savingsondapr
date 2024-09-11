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
        app.MapPost("v1/accounts/:handle-iasaactivated-event",
            [Topic("pubsub", "instantaccesssavingsaccountactivated")] (InstantAccessSavingsAccountActivated @event, ILogger<PlatformModule> logger) =>
            {
                logger.LogInformation("Received IASA activated event: {accountId}", @event.AccountId);
            });

        app.MapPost("/v1/commands",
                    [Topic("pubsub", "commands")] async (PubSubCommand evt, IMediator mediator, ILogger<PlatformModule> logger) =>
                    {
                        try
                        {
                            if (evt is not null && evt.Data is not null)
                            {
                                var cmdString = JsonSerializer.Serialize(evt.Data);
                                var type = Type.GetType(evt.CommandType, true);
                                var cmd = JsonSerializer.Deserialize(evt.Data, type);
                                await mediator.Send(cmd);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError($"Error processing command: {ex.Message}");
                            throw;
                        }
                    });

        app.MapPost("/publish-events",
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
        });

        app.MapMethods("/publish-events", ["OPTIONS"],
            () => Task.FromResult(Results.Accepted()));

        app.MapGet("/v1/savings-account/command/{msgid}", async (string msgid, IStateEntryRepository<InstantAccessSavingsAccountState> repo) =>
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
        });

    }
}
