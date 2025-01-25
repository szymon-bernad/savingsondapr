using Carter;
using SavingsPlatform.Accounts.AccountHolders;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Requests;

namespace SavingsOnDapr.Api.ApiModules;

public class AccountHolderModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/account-holders/{refid}", async (string refid, IStateEntryQueryHandler<AccountHolderState> repo) =>
        {
        var result = await repo.GetAccountAsync(refid);
            return Results.Ok(result);
        }).WithTags(["account-holders"]);


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

                return Results.CreatedAtRoute($"/api/account-holders/{request.Id}");

            }).WithTags(["account-holders"]);
    }
}
