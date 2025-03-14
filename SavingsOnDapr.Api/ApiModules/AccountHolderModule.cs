﻿using Carter;
using SavingsPlatform.Accounts.AccountHolders;
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
                        new AccountHolderResponse(result.Key, result?.Username ?? string.Empty, result?.AccountIds ?? []));
                }    

                return Results.NotFound();
            })
            .WithTags(["account-holders"]);


        app.MapPost("/api/account-holders",
            async (IAggregateRootFactory<AccountHolder, AccountHolderState> factory,
                   CreateAccountHolder request) =>
            {
                var ahResult = await factory.TryGetInstanceAsync(request.Id);

                if (ahResult is not null)
                {
                    return Results.BadRequest("Account Holder already exists.");
                }

                var accountHolder = await factory.GetInstanceAsync();
                await accountHolder.Create(request.Id, request.Username, request.ExternalRef, request.AccountIds);

                return Results.Created($"/api/account-holders/{request.Id}", null);

            })
            .WithTags(["account-holders"]);
    }
}
