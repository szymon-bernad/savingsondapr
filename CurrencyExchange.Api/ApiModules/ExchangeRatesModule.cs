using Carter;
using CurrencyExchange.Api.Internal;
using Dapr.Client;
using Dapr.Workflow;
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

        app.MapPost("v1/currency-exchange-rate", async (CurrencyExchangeBaseRateRequest req, IExchangeRatesService exchangeRatesService) =>
        {
            var exchangeRates = await exchangeRatesService.SetExchangeBaseRateAsync(req.Source, req.Target, req.BaseRate);
            return Results.Ok(exchangeRates);
        }).WithTags(["exchange-rates"]);

        app.MapPost("v1/currency-exchange-order", async (CurrencyExchangeOrder request, DaprWorkflowClient wfClient) =>
        {
            await wfClient.ScheduleNewWorkflowAsync(nameof(CurrencyExchangeWorkflow), request.OrderId, request);

            await wfClient.WaitForWorkflowStartAsync(request.OrderId);

           return Results.Accepted($"v1/currency-exchange-order/{request.OrderId}");
        }).WithTags(["currency-exchange"]);

        app.MapGet("v1/currency-exchange-order/{orderId}", async (string orderId, DaprWorkflowClient wfClient) =>
        {
            var state = await wfClient.GetWorkflowStateAsync(orderId);

            if (state is null)
            {
                return Results.NotFound();
            }
            else if (state.IsWorkflowCompleted)
            {
                return Results.Ok(
                    new 
                    {
                        Status = Enum.GetName<WorkflowRuntimeStatus>(state.RuntimeStatus),
                        Details = state.ReadOutputAs<ExchangeResult>()
                    });
            }

            return Results.Accepted($"v1/currency-exchange-order/{orderId}");
        }).WithTags(["currency-exchange"]);

        app.MapGet("/healthz", () => Results.Ok()).WithTags(["platform"]);
    }
}
