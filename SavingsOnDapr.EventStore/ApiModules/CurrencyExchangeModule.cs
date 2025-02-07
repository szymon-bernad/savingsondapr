using Carter;
using SavingsOnDapr.EventStore.Store;
using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsOnDapr.EventStore.ApiModules;

public class CurrencyExchangeModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("v1/events/currency-exchange-summary/{source:alpha}/{target:alpha}/{forDate:datetime}",
            async (CurrencyExchangeEventStore store, Currency source, Currency target, DateTime forDate) =>
            {
                var result = await store.GetSummary($"{source}=>{target}_{forDate:yyyy-MM-dd}");

                return Results.Ok(result);
            });
    }
}
