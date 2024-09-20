using Carter;
using SavingsPlatform.Accounts.Aggregates.InstantAccess;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Common.Services;
using SavingsPlatform.Contracts.Accounts.Commands;
using SavingsPlatform.Contracts.Accounts.Requests;

namespace SavingsOnDapr.Api.ApiModules;

public class SavingsModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/savings/savings-account/{refid}", async (string refid, IStateEntryRepository<InstantAccessSavingsAccountState> repo) =>
        {
            var result = await repo.QueryAccountsByKeyAsync(new string[] { "data.externalRef" }, new string[] { refid });
            return Results.Ok(result);
        }).WithTags(["savings"]);

        app.MapPost("/api/savings/savings-accounts",
            async (IEventPublishingService publishingService,
                    IStateEntryRepository<InstantAccessSavingsAccountState> repo,
                   CreateSavingsAccount request) =>
            {
                var result = await repo.QueryAccountsByKeyAsync(new string[] { "data.externalRef" }, new string[] { request.ExternalRef });

                if(result.Any())
                {
                    return Results.BadRequest("Account already exists");
                }

                await publishingService.PublishCommand(new CreateInstantSavingsAccountCommand(request.ExternalRef, request.InterestRate, request.PlatformId));

                return Results.Accepted($"/api/savings/savings-account/{request.ExternalRef}");
            }).WithTags(["savings"]);

        app.MapPost("/api/savings/savings-account/:credit",
            async (IEventPublishingService publishingService,
                    CreditAccount request) =>
            {
                var cmdId = request.MsgId ?? Guid.NewGuid().ToString();
                await publishingService.PublishCommand(
                    new CreditAccountCommand(cmdId, request.ExternalRef, request.Amount, DateTime.UtcNow, request.TransferRef));

                return Results.Accepted($"/api/savings/savings-account/command/{cmdId}");
            }).WithTags(["savings"]);

        app.MapPost("/api/savings/savings-account/:debit",
            async (IEventPublishingService publishingService,
                    DebitAccount request) =>
            {
                var cmdId = request.MsgId ?? Guid.NewGuid().ToString();
                await publishingService.PublishCommand(
                    new DebitAccountCommand(cmdId, request.ExternalRef, request.Amount, DateTime.UtcNow, request.TransferRef));

                return Results.Accepted($"/v1/savings-account/command/{cmdId}");
            }).WithTags(["savings"]);
    }
}
