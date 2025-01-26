using Carter;
using Dapr.Client;
using Microsoft.Extensions.Logging;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Current.Models;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Common.Services;
using SavingsPlatform.Contracts.Accounts.Commands;
using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Models;
using SavingsPlatform.Contracts.Accounts.Requests;

namespace SavingsOnDapr.Api.ApiModules;

public class CurrentAccountsModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/accounts/{refid}", async (string refid, IStateEntryQueryHandler<CurrentAccountState> repo) =>
        {
            var result = await repo.QueryAccountsByKeyAsync(["externalRef"], [refid]);
            if (!result.Any())
            {
                return Results.NotFound();
            }

            var res = result.First();
            return Results.Ok(
                new CurrentAccountResponse(res.Key, res.ExternalRef, res.OpenedOn, res.TotalBalance, res.Currency, AccountType.CurrentAccount));
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

        app.MapPost("/api/accounts/:query-by-ids",
            async (IStateEntryQueryHandler<CurrentAccountState> repo,
                    string[] accountIds) =>
            {
                var result = await repo.GetAccountsAsync(accountIds);
                return Results.Ok(result.Select(x => new CurrentAccountResponse(x.Key, x.ExternalRef, x.OpenedOn, x.TotalBalance, x.Currency, AccountType.CurrentAccount)));
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
