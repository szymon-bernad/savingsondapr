using Carter;
using Dashboard.Api.ApiClients;
using SavingsPlatform.Contracts.Accounts.Models;
using System.Security.Claims;

namespace Dashboard.Api.ApiModules;

public class AccountsModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/{userid}/accounts", 
            async (
            string userid,
            IAccountsApiClient apiClient,
            ClaimsPrincipal user) =>
        {
            if (user.Identity.IsAuthenticated)
            {
                var oidClaim = user.Claims?.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier");
                if (oidClaim?.Value == userid)
                {
                    var accounts = await apiClient.GetAllUserAccountsAsync(userid);
                    return Results.Ok(accounts);
                }
            }

            return Results.Forbid();
        })
        .RequireAuthorization(["ValidateAccessTokenPolicy"])
        .WithTags(["users-accounts"]);
    }
}
