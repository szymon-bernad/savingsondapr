using Carter;
using Microsoft.Extensions.Logging;
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
        app.MapGet("/api/accounts/{refid}", async (string refid, IStateEntryQueryHandler<CurrentAccountState> repo) =>
        {
            var result = await repo.QueryAccountsByKeyAsync(["externalRef"], [refid]);
            return Results.Ok(result);
        }).WithTags(["accounts"]);

        app.MapPost("/api/accounts",
            async (IEventPublishingService publishingService,
                   IStateEntryQueryHandler<CurrentAccountState> caRepo,
                   CreateCurrentAccount request) =>
            {

                var caResult = await caRepo.QueryAccountsByKeyAsync(["externalRef"], [request.ExternalRef]);
                if (caResult.Any())
                {
                    return Results.BadRequest("Current Account already exists.");
                }

                await publishingService.PublishCommand(new CreateCurrentAccountCommand(Guid.NewGuid().ToString(), request.ExternalRef, request.AccountCurrency));

                return Results.Accepted($"/api/accounts/{request.ExternalRef}");
            }).WithTags(["accounts"]);

        app.MapPost("/api/accounts/:credit",
            async (IEventPublishingService publishingService,
                    CreditAccount request,
                    ILogger<CurrentAccountsModule> logger) =>
            {
                var cmdId = request.MsgId ?? Guid.NewGuid().ToString();
                logger.LogInformation($"Processing credit request for {request.ExternalRef} with CmdId = {cmdId}.");

                await publishingService.PublishCommand(
                    new CreditAccountCommand(cmdId, request.ExternalRef, request.Amount, DateTime.UtcNow, AccountType.CurrentAccount, request.TransferId));

                return Results.Accepted($"/api/platform/accounts/command/{cmdId}");
            }).WithTags(["accounts"]);

        app.MapPost("/api/accounts/:debit",
            async (IEventPublishingService publishingService,
                    DebitAccount request,
                    ILogger<CurrentAccountsModule> logger) =>
            {
                var cmdId = request.MsgId ?? Guid.NewGuid().ToString();
                logger.LogInformation($"Processing debit request for {request.ExternalRef} with CmdId = {cmdId}.");
                await publishingService.PublishCommand(
                    new DebitAccountCommand(cmdId, request.ExternalRef, request.Amount, DateTime.UtcNow, AccountType.CurrentAccount, request.TransferId));

                return Results.Accepted($"/api/platform/accounts/command/{cmdId}");
            }).WithTags(["accounts"]);
    }
}
