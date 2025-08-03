using Carter;
using Dashboard.Api.ApiClients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using SavingsPlatform.Contracts.Accounts.Commands;
using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Models;
using SavingsPlatform.Contracts.Accounts.Requests;
using SavingsPlatform.Contracts.CurrencyExchange.Response;
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
                    ICollection<BaseAccountResponse>? accounts = default; 
                   // var cachedValue = await cache.GetStringAsync($"accounts-{userid}");
                   // if (!string.IsNullOrWhiteSpace(cachedValue))
                   // {
                   //     try
                   //     {
                   //         accounts = JsonSerializer.Deserialize<ICollection<BaseAccountResponse>>(cachedValue);
                   //     }
                   //     catch (Exception ex)
                   //     {
                   //         cachedValue = null;
                   //     }
                   // }

                    accounts ??= await apiClient.GetAllUserAccountsAsync(userid);

                    // if ((accounts?.Any() ?? false) && string.IsNullOrWhiteSpace(cachedValue))
                    // {
                    //     var cacheKey = $"accounts-{userid}";
                    //     var cacheOptions = new DistributedCacheEntryOptions
                    //     {
                    //         AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                    //     };
                    //     await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(accounts), cacheOptions);
                    // }
                    // 
                    return Results.Ok(accounts);
                }
            }

            return Results.Forbid();
        })
        .RequireAuthorization()
        .WithTags(["users-accounts"]);

        app.MapPost("/api/users/{userid}/:add-user-account",
            async (
                string userid,
                IAccountsApiClient apiClient,
                ClaimsPrincipal user,
                [FromBody] CreateAccountRequest request
                ) =>
            {
                if (user.Identity?.IsAuthenticated ?? false)
                {
                    var oidClaim = user.Claims?.FirstOrDefault(c => c.Type == OidClaimType);
                    if (oidClaim?.Value == userid)
                    {
                        await apiClient.AddUserAccountAsync(request);
                        return Results.NoContent();
                    }
                }
                return Results.Forbid();
            })
            .RequireAuthorization()
            .WithTags(["users-accounts"]);

        app.MapGet("/api/savings/{currency}/interest-rate",
            async (string currency,
            IAccountsApiClient apiClient) =>
            {
                return Results.Ok(await apiClient.GetSavingsInterestRateAsync(Enum.Parse<Currency>(currency)));
            })
            .Produces<CurrencyRateResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status500InternalServerError);

        app.MapGet("/healthz", () => Results.Ok()).WithTags(["platform"]);
    }
}
