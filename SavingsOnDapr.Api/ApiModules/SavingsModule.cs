using Carter;
using Microsoft.JSInterop;
using SavingsPlatform.Accounts.Aggregates.InstantAccess;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Current.Models;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Common.Services;
using SavingsPlatform.Contracts.Accounts.Commands;
using SavingsPlatform.Contracts.Accounts.Enums;
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
                   IStateEntryRepository<InstantAccessSavingsAccountState> iasaRepo,
                   IStateEntryRepository<CurrentAccountState> caRepo,
                   CreateSavingsAccount request) =>
            {
                var result = await iasaRepo.QueryAccountsByKeyAsync(["data.externalRef"], [request.ExternalRef]);

                if(result.Any())
                {
                    return Results.BadRequest("Savings Account already exists");
                }

                var caResult = await caRepo.QueryAccountsByKeyAsync(["data.externalRef"], [request.CurrentAccountRef]);
                if(!caResult.Any())
                {
                    return Results.BadRequest("Current Account not found");
                }

                await publishingService.PublishCommand(new CreateInstantSavingsAccountCommand(Guid.NewGuid().ToString(), request.ExternalRef, request.InterestRate, caResult.First().Key));

                return Results.Accepted($"/api/savings/savings-account/{request.ExternalRef}");
            }).WithTags(["savings"]);

        app.MapPost("/api/savings/savings-account/:credit",
            async (IEventPublishingService publishingService,
                    CreditAccount request) =>
            {
                var cmdId = request.MsgId ?? Guid.NewGuid().ToString();
                var transferCmd = new TransferDepositCommand(request.ExternalRef, request.TransactionDate, request.Amount, TransferType.CurrentToSavings, cmdId, cmdId);
                await publishingService.PublishCommand(transferCmd);
                return Results.Accepted($"/api/platform/savings-account/command/{cmdId}");
            }).WithTags(["savings"]);

        app.MapPost("/api/savings/savings-account/:debit",
            async (IEventPublishingService publishingService,
                    DebitAccount request) =>
            {
                var cmdId = request.MsgId ?? Guid.NewGuid().ToString();
                var transferCmd = new TransferDepositCommand(request.ExternalRef, request.TransactionDate, request.Amount, TransferType.SavingsToCurrent, cmdId, cmdId);
                await publishingService.PublishCommand(transferCmd);
                return Results.Accepted($"/api/platform/savings-account/command/{cmdId}");
            }).WithTags(["savings"]);
    }
}
