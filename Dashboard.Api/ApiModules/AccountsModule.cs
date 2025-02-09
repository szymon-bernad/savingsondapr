using Carter;
using Dashboard.Api.ApiClients;
using System.Security.Claims;

namespace Dashboard.Api.ApiModules;

public class AccountsModule : ICarterModule
{
    private const string OidClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/{userid}/accounts", 
            async (
                string userid,
                IAccountsApiClient apiClient,
                ClaimsPrincipal user) => 
        {
            if (user.Identity?.IsAuthenticated ?? false)
            {
                var oidClaim = user.Claims?.FirstOrDefault(c => c.Type == OidClaimType);
                if (oidClaim?.Value == userid)
                {
                    var accounts = await apiClient.GetAllUserAccountsAsync(userid);
                    return Results.Ok(accounts);
                }
            }

            return Results.Forbid();
        })
        .RequireAuthorization()
        .WithTags(["users-accounts"]);

        app.MapGet("/healthz", () => Results.Ok()).WithTags(["platform"]);
    }
}
