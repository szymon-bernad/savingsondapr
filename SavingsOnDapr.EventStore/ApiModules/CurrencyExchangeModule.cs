using Carter;
using Microsoft.AspNetCore.Mvc;
using SavingsOnDapr.EventStore.Store;
using SavingsPlatform.Common.Services;
using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Requests;

namespace SavingsOnDapr.EventStore.ApiModules;

public class CurrencyExchangeModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("v1/currency-exchange-summary/:init",
            async ([FromServices]IEventPublishingService pubService,
            [FromBody]CurrencyExchangeSummaryRequest request) =>
        {
            await pubService.PublishEventsToTopic("summaryrequests", [request]);
            return Results.Accepted();
        });

        app.MapGet("v1/events/currency-exchange-summary/{source:alpha}/{target:alpha}/{forDate:datetime}",
            async (CurrencyExchangeEventStore store, Currency source, Currency target, DateTime forDate, [FromQuery]DateTime? toDate = null) =>
            {

                ICollection<string> streamIds = new List<string> { $"{source}=>{target}_{forDate:yyyy-MM-dd}" };

                if (toDate is not null)
                {
                    var itDate = forDate.Date.AddDays(1);
                    while(itDate <= toDate.Value.Date)
                    {
                        streamIds.Add($"{source}=>{target}_{itDate:yyyy-MM-dd}");
                        itDate = itDate.AddDays(1);
                    }
                }

                var result = await Task.WhenAll(streamIds.Select(streamId => store.GetSummary(streamId)));
                return Results.Ok(result);
            });
    }
}
