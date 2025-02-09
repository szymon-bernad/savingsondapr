using Carter;
using Dashboard.Api.ApiClients;
using SavingsPlatform.Contracts.Accounts.Requests;
using System.Security.Claims;

namespace Dashboard.Api.ApiModules;

public class ExchangeModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/currency/exchange-order",
            async (
                IAccountsApiClient accountsApiClient,
                IExchangeApiClient exchangeApiClient,
                CurrencyExchangeOrder order,
                ClaimsPrincipal user) =>
            {
                if (user.Identity?.IsAuthenticated ?? false)
                {
                    var oidClaim = user.Claims?.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier");
                    if (oidClaim?.Value is not null)
                    {
                        var accounts = await accountsApiClient.GetAllUserAccountsAsync(oidClaim!.Value);
                        if (accounts.Any(a => a.ExternalRef == order.DebtorExternalRef))
                        {
                            await exchangeApiClient.ScheduleExchangeOrderAsync(order);
                            return Results.Accepted();
                        }
                    }
                }
                return Results.Forbid();
            })
        .RequireAuthorization()
        .WithTags(["exchange"]);

        app.MapPost("/api/currency/exchange-query",
            async (
                IExchangeApiClient exchangeApiClient,
                CurrencyExchangeQuery query) =>
            {
                var result = await exchangeApiClient.GetExchangeRateAsync(query);
                return Results.Ok(result);
            })
        .RequireAuthorization()
        .WithTags(["exchange"]);
    }


}
