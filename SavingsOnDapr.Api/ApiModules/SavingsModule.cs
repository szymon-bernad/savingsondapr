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
        app.MapGet("/v1/savings-account/{refid}", async (string refid, IStateEntryRepository<InstantAccessSavingsAccountState> repo) =>
        {
            var result = await repo.QueryAccountsByKeyAsync(new string[] { "data.externalRef" }, new string[] { refid });
            return Results.Ok(result);
        });

        app.MapPost("/v1/savings-accounts",
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

                return Results.Accepted($"/v1/savings-account/{request.ExternalRef}");
            });

        app.MapPost("/v1/savings-account/:credit",
            async (IEventPublishingService publishingService,
                    CreditAccount request) =>
            {
                var cmdId = request.MsgId ?? Guid.NewGuid().ToString();
                await publishingService.PublishCommand(
                    new CreditAccountCommand(cmdId, request.ExternalRef, request.Amount, DateTime.UtcNow, request.TransferRef));

                return Results.Accepted($"/v1/savings-account/command/{cmdId}");
            });

        app.MapPost("/v1/savings-account/:debit",
            async (IEventPublishingService publishingService,
                    DebitAccount request) =>
            {
                var cmdId = request.MsgId ?? Guid.NewGuid().ToString();
                await publishingService.PublishCommand(
                    new DebitAccountCommand(cmdId, request.ExternalRef, request.Amount, DateTime.UtcNow, request.TransferRef));

                return Results.Accepted($"/v1/savings-account/command/{cmdId}");
            });
    }
}
