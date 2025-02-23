using Carter;
using Dashboard.Api.ApiClients;
using Microsoft.Extensions.Caching.Distributed;
using SavingsPlatform.Contracts.Accounts.Models;
using System.Security.Claims;
using System.Text.Json;

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
                ClaimsPrincipal user,
                IDistributedCache cache) => 
        {
            if (user.Identity?.IsAuthenticated ?? false)
            {
                var oidClaim = user.Claims?.FirstOrDefault(c => c.Type == OidClaimType);
                if (oidClaim?.Value == userid)
                {
                    ICollection<CurrentAccountResponse>? accounts = default; 
                    var cachedValue = await cache.GetStringAsync($"accounts-{userid}");
                    if (!string.IsNullOrWhiteSpace(cachedValue))
                    {
                        try
                        {
                            accounts = JsonSerializer.Deserialize<ICollection<CurrentAccountResponse>>(cachedValue);
                        }
                        catch (Exception ex)
                        {
                            cachedValue = null;
                        }
                    }

                    accounts ??= await apiClient.GetAllUserAccountsAsync(userid);

                    if ((accounts?.Any() ?? false) && string.IsNullOrWhiteSpace(cachedValue))
                    {
                        var cacheKey = $"accounts-{userid}";
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                        };
                        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(accounts), cacheOptions);
                    }

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
