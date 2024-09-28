using Carter;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Current.Models;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Common.Services;
using SavingsPlatform.Contracts.Accounts.Commands;
using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Requests;

namespace SavingsOnDapr.Api.ApiModules;

public class CurrentAccountsModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/accounts/{refid}", async (string refid, IStateEntryRepository<CurrentAccountState> repo) =>
        {
            var result = await repo.QueryAccountsByKeyAsync(["data.externalRef"], [refid]);
            return Results.Ok(result);
        }).WithTags(["accounts"]);

        app.MapPost("/api/accounts",
            async (IEventPublishingService publishingService,
                   IStateEntryRepository<CurrentAccountState> caRepo,
                   CreateCurrentAccount request) =>
            {

                var caResult = await caRepo.QueryAccountsByKeyAsync(["data.externalRef"], [request.ExternalRef]);
                if (caResult.Any())
                {
                    return Results.BadRequest("Current Account already exists.");
                }

                await publishingService.PublishCommand(new CreateCurrentAccountCommand(Guid.NewGuid().ToString(), request.ExternalRef));

                return Results.Accepted($"/api/accounts/{request.ExternalRef}");
            }).WithTags(["accounts"]);

        app.MapPost("/api/accounts/:credit",
            async (IEventPublishingService publishingService,
                    CreditAccount request) =>
            {
                var cmdId = request.MsgId ?? Guid.NewGuid().ToString();
                await publishingService.PublishCommand(
                    new CreditAccountCommand(cmdId, request.ExternalRef, request.Amount, DateTime.UtcNow, AccountType.CurrentAccount, null));

                return Results.Accepted($"/api/platform/savings-account/command/{cmdId}");
            }).WithTags(["accounts"]);

        app.MapPost("/api/accounts/:debit",
            async (IEventPublishingService publishingService,
                    DebitAccount request) =>
            {
                var cmdId = request.MsgId ?? Guid.NewGuid().ToString();
                await publishingService.PublishCommand(
                    new DebitAccountCommand(cmdId, request.ExternalRef, request.Amount, DateTime.UtcNow, AccountType.CurrentAccount, null));

                return Results.Accepted($"/api/platform/savings-account/command/{cmdId}");
            }).WithTags(["accounts"]);
    }
}
