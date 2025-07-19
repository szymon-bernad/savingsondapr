using Carter;
using SavingsPlatform.Accounts.AccountHolders;
using SavingsPlatform.Accounts.Handlers;
using SavingsPlatform.Common.Helpers;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Models;
using SavingsPlatform.Contracts.Accounts.Requests;

namespace SavingsOnDapr.Api.ApiModules;

public class AccountHolderModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/account-holders/{refid}", async (string refid, IStateEntryQueryHandler<AccountHolderState> repo) =>
            {
                var result = await repo.GetAccountAsync(refid);
                if (result is not null)
                {
                    return Results.Ok(
                        new AccountHolderResponse(result.Key, result?.Username ?? string.Empty, result?.Accounts ?? []));
                }    

                return Results.NotFound();
            })
            .WithTags(["account-holders"]);


        app.MapPost("/api/account-holders",
            async (IAggregateRootFactory<AccountHolder, AccountHolderState> factory,
                   IAccountsQueryHandler accountsQueryHandler,
                   CreateAccountHolder request) =>
            {
                var ahResult = await factory.TryGetInstanceAsync(request.Id);

                if (ahResult is not null)
                {
                    return Results.BadRequest("Account Holder already exists.");
                }

                var accountHolder = await factory.GetInstanceAsync();
                var accountInfos = await accountsQueryHandler.FetchAccountInfosByIds(request.AccountIds);

                await accountHolder.Create(request.Id, request.Username, request.ExternalRef, accountInfos);

                return Results.Created($"/api/account-holders/{request.Id}", null);

            })
            .WithTags(["account-holders"]);

        app.MapPost("/api/account-holders/{refid}/accounts",
            async (IAggregateRootFactory<AccountHolder, AccountHolderState> factory,
                   IAccountsQueryHandler accountsQueryHandler,
                   string refid,
                   ICollection<string> accountIds) =>
            {
                if (accountIds is null || accountIds.Count == 0)
                {
                    return Results.BadRequest("No account IDs provided.");
                }

                var ahResult = await factory.TryGetInstanceAsync(refid);
                if (ahResult is null)
                {
                    return Results.NotFound("Account Holder not found.");
                }

                var accountInfos = await accountsQueryHandler.FetchAccountInfosByIds([.. accountIds]);

                await ahResult.AddAccounts(accountInfos);
                return Results.Accepted();
            })
            .WithTags(["account-holders"]);
    }
}
