using Carter;
using Dashboard.Api.ApiClients;
using System.Security.Claims;

namespace Dashboard.Api.ApiModules;

public class AccountsModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/{userid}/accounts", async (
            string userid,
            IAccountsApiClient apiClient,
            ClaimsPrincipal claims) =>
        {
           var accounts = await apiClient.GetAllUserAccountsAsync(userid);

            return Results.Ok(accounts);
        }).WithTags(["users-accounts"]);
    }
}
