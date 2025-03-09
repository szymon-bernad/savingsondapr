using Carter;
using Dashboard.Api.ApiClients;
using JasperFx.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.ObjectPool;
using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Models;
using SavingsPlatform.Contracts.Accounts.Requests;
using SavingsPlatform.Contracts.CurrencyExchange.Response;
using System.Security.Claims;
using System.Text.Json;

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


        app.MapGet("/api/currency-exchange-summary/{source:alpha}/{target:alpha}/{forDate:datetime}",
            async (IDistributedCache cache,
                    Currency source,
                    Currency target,
                    DateTime forDate,
                    [FromQuery] DateTime? toDate = null) =>
            {
                var summaryKey = toDate.HasValue ?
                    $"{source}=>{target}_{forDate:yyyy-MM-dd}_{toDate:yyyy-MM-dd}" :
                    $"{source}=>{target}_{forDate:yyyy-MM-dd}";

                var cachedValue = await cache.GetStringAsync(summaryKey);
                if (string.IsNullOrWhiteSpace(cachedValue))
                {
                    await cache.SetStringAsync(summaryKey, "loading", new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
                    });
                    return Results.Accepted();
                }
                else if (string.Equals(cachedValue, "loading", StringComparison.OrdinalIgnoreCase))
                {
                    var res = GetFakeResponse(summaryKey, source, target, forDate, toDate);
                    await cache.SetStringAsync(summaryKey, JsonSerializer.Serialize(res), new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60)
                    });
                    return Results.Ok(res);
                }
                else
                {
                    try
                    {
                        var cachedResult = JsonSerializer.Deserialize<CurrencyExchangeSummaryResponse>(cachedValue);
                        return Results.Ok(cachedResult);
                    }
                    catch (Exception ex)
                    {
                        cachedValue = null;
                        await cache.RemoveAsync(summaryKey);
                        return Results.StatusCode(500);
                    }
                }
            });
    }

    private static CurrencyExchangeSummaryResponse GetFakeResponse(
                string summaryKey,
                Currency source,
                Currency target,
                DateTime forDate,
                [FromQuery] DateTime? toDate = null)
    {
        var rnd = new Random();

        ICollection<string> dateEntries = new List<string> { $"{forDate:yyyy-MM-dd}" };
        if (toDate is not null)
        {
            var itDate = forDate.Date.AddDays(1);
            while (itDate <= toDate.Value.Date)
            {
                dateEntries.Add($"{itDate:yyyy-MM-dd}");
                itDate = itDate.AddDays(1);
            }
        }

        IDictionary<string, string[]> columnValues = new Dictionary<string, string[]>();

        foreach (var date in dateEntries)
        {
            var count = rnd.Next(10_000);
            columnValues.Add(date, [date,
                             $"{count}",
                             $"{Decimal.Round((decimal)(rnd.NextDouble()*count*123.00), 2)}",
                             $"{Decimal.Round((decimal)(rnd.NextDouble()*count*97.00), 2)}"]);
        }

        return new CurrencyExchangeSummaryResponse
        {
            ResponseKey = summaryKey,
            ColumnNames = ["Date", "TotalExchangesCount", "TotalSourceAmount", "TotalTargetAmount"],
            ColumnValues = columnValues
        };
    }

}
