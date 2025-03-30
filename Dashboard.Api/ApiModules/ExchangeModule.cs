using Azure;
using Carter;
using Dapr.Client;
using Dashboard.Api.ApiClients;
using JasperFx.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.ObjectPool;
using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Models;
using SavingsPlatform.Contracts.Accounts.Requests;
using SavingsPlatform.Contracts.CurrencyExchange.Response;
using System.Security.Claims;
using System.Text.Json;
using static Dapr.Client.Autogen.Grpc.v1.Dapr;
using DaprClient = Dapr.Client.DaprClient;

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

        app.MapPost("/api/currency-exchange-summary/{source:alpha}/{target:alpha}/{forDate:datetime}",
            async (
                    HttpRequest request,
                    Currency source,
                    Currency target,
                    DateTime forDate,
                     [FromServices] IEventStoreApiClient apiClient,
                    [FromServices] DaprClient daprClient,
                   [FromServices] IDistributedCache cache,
                    [FromQuery] DateTime? toDate = null) =>
            {
                if (forDate.Date > DateTime.UtcNow.Date)
                {
                    return Results.BadRequest("Cannot generate summary for future dates.");
                }

                if (toDate.HasValue && toDate.Value.Date > DateTime.UtcNow.Date)
                {
                    return Results.BadRequest("Invalid date range provided.");
                }

                if (source == target)
                {
                    return Results.BadRequest("Source and target currencies cannot be the same.");
                }

                var summaryKey = toDate.HasValue ?
                    $"{source}=>{target}_{forDate:yyyy-MM-dd}_{toDate:yyyy-MM-dd}" :
                    $"{source}=>{target}_{forDate:yyyy-MM-dd}_{forDate:yyyy-MM-dd}";

                var cachedValue = await cache.GetStringAsync(summaryKey);

                if (!string.IsNullOrWhiteSpace(cachedValue))
                {
                    try
                    {
                        var cachedResult = JsonSerializer.Deserialize<CurrencyExchangeSummaryResponse>(cachedValue);

                        return Results.Accepted(
                            request.GetEncodedPathAndQuery());
                    }
                    catch (Exception ex)
                    {
                        await cache.RemoveAsync(summaryKey);
                    }
                }

                var flagValue = false;

                try
                {
                    var cfg = await daprClient.GetConfiguration("app-cfg", ["get-rnd-exch"]);

                    if (cfg.Items.TryGetValue("get-rnd-exch", out var genRandomExchangeSummary) &&
                        bool.TryParse(genRandomExchangeSummary.Value, out var genRandomExchangeSummaryValue))
                    {
                        flagValue = genRandomExchangeSummaryValue;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    flagValue = true;
                }

                if (flagValue)
                {
                    var res = GetFakeResponse(summaryKey, source, target, forDate, toDate);
                    await cache.SetStringAsync(summaryKey, JsonSerializer.Serialize(res), new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60)
                    });

                }
                else
                {
                    var startDate = new DateOnly(forDate.Year, forDate.Month, forDate.Day);
                    var endDate = toDate.HasValue ? new DateOnly(toDate.Value.Year, toDate.Value.Month, toDate.Value.Day) : startDate;

                    await apiClient.InitiateCurrencyExchangeSummaryAsync(new CurrencyExchangeSummaryRequest
                    {
                        SourceCurrency = source,
                        TargetCurrency = target,
                        StartDate = startDate,
                        EndDate = endDate
                    });
                }

                return Results.Accepted(request.GetEncodedPathAndQuery());
            })
            .Produces(StatusCodes.Status202Accepted)
            .Produces(StatusCodes.Status400BadRequest);

        app.MapGet("/api/currency-exchange-summary/{source:alpha}/{target:alpha}/{forDate:datetime}",
            async (IDistributedCache cache,
                    Currency source,
                    Currency target,
                    DateTime forDate,
                    [FromQuery] DateTime? toDate = null) =>
            {
                if (forDate.Date > DateTime.UtcNow.Date)
                {
                    return Results.BadRequest("Cannot generate summary for future dates.");
                }

                if (toDate.HasValue && toDate.Value.Date > DateTime.UtcNow.Date)
                {
                    return Results.BadRequest("Invalid date range provided.");
                }

                if (source == target)
                {
                    return Results.BadRequest("Source and target currencies cannot be the same.");
                }

                var summaryKey = toDate.HasValue ?
                    $"{source}=>{target}_{forDate:yyyy-MM-dd}_{toDate:yyyy-MM-dd}" :
                    $"{source}=>{target}_{forDate:yyyy-MM-dd}_{forDate:yyyy-MM-dd}";

                var cachedValue = await cache.GetStringAsync(summaryKey);
                if (string.IsNullOrWhiteSpace(cachedValue))
                {
                    return Results.NoContent();
                }
                else
                {
                    try
                    {
                        var cachedResult = JsonSerializer.Deserialize<CurrencyExchangeSummaryResponse>(cachedValue);
                        return Results.Ok(cachedResult);
                    }
                    catch (Exception)
                    {
                        await cache.RemoveAsync(summaryKey);
                        return Results.StatusCode(500);
                    }
                }
            })
            .Produces<CurrencyExchangeSummaryResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status500InternalServerError);
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

        var entries = new List<CurrencyExchangeSummaryValueEntry>();

        foreach (var date in dateEntries)
        {
            var count = rnd.Next(10_000);
            entries.Add(new CurrencyExchangeSummaryValueEntry
            {
                EntryName = date,
                ColumnValues =
                        [date,
                         $"{count}",
                         $"{Decimal.Round((decimal)(rnd.NextDouble()*count*123.00), 2)}",
                         $"{Decimal.Round((decimal)(rnd.NextDouble()*count*97.00), 2)}"]
            });
        }

        return new CurrencyExchangeSummaryResponse
        {
            ResponseKey = summaryKey,
            ColumnNames = ["Date", "TotalExchangesCount", "TotalSourceAmount", "TotalTargetAmount"],
            Entries = entries.ToArray(),
        };
    }

}
