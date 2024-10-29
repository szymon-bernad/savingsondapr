using Carter;
using CurrencyExchange.Api.Internal;
using SavingsPlatform.Contracts.Accounts.Models;
using SavingsPlatform.Contracts.Accounts.Requests;

namespace CurrencyExchange.Api.ApiModules;

public class ExchangeRatesModule : ICarterModule
{

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("v1/currency-exchange-query", async (CurrencyExchangeQuery query, IExchangeRatesService exchangeRatesService) =>
        {
            var exchangeRates = await exchangeRatesService.GetCurrentRatesAsync(query.Source, query.Target);
            var result = exchangeRates.ExchangeRates.FirstOrDefault(x => x.IsInRange(query.Amount));

            if (result == null)
            {
                return Results.BadRequest(new { Message = "No exchange rate found for the given amount." });
            }

            return Results.Ok(
                new CurrencyExchangeResponse(
                    Math.Round(query.Amount * result.Rate, 2, MidpointRounding.ToEven),
                    result.Rate,
                    $"{query.Source} => {query.Target}",
                    DateTime.UtcNow));

        }).WithTags(["exchange-rates"]);
    }

}
