using Dapr.Workflow;
using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Requests;

namespace CurrencyExchange.Api.Internal.Activities;

public class ConfirmExchangeActivity(IExchangeRatesService exchangeRatesService) 
    : WorkflowActivity<CurrencyExchangeOrder, OrderConfirmationResult>
{
    private readonly IExchangeRatesService _exchangeRatesService = exchangeRatesService;

    public override Task<OrderConfirmationResult> RunAsync(WorkflowActivityContext context, CurrencyExchangeOrder input)
    {
        var result = input.OrderType switch
        {
            ExchangeOrderType.MarketRate     => RunForMarketRate(input),
            ExchangeOrderType.LimitOrder     => RunForLimitOrder(input),
            _ => throw new NotImplementedException(),
        };

        return result;
    }

    private async Task<OrderConfirmationResult> RunForMarketRate(CurrencyExchangeOrder input)
    {
        var rates = await _exchangeRatesService.GetCurrentRatesAsync(input.SourceCurrency, input.TargetCurrency);
        var applicableRate = rates.ExchangeRates.FirstOrDefault(x => x.IsInRange(input.SourceAmount));

        if (applicableRate is not null)
        {
            return new OrderConfirmationResult(
                ConfirmationStatus.Confirmed,
                Guid.NewGuid().ToString(),
                applicableRate.Rate,
                Math.Round(applicableRate.Rate * input.SourceAmount, 2, MidpointRounding.ToEven));
        }

        return new OrderConfirmationResult(ConfirmationStatus.Rejected);
    }

    private async Task<OrderConfirmationResult> RunForLimitOrder(CurrencyExchangeOrder input)
    {
        var rates = await _exchangeRatesService.GetCurrentRatesAsync(input.SourceCurrency, input.TargetCurrency);
        var applicableRate = rates.ExchangeRates.FirstOrDefault(x => x.IsInRange(input.SourceAmount));

        if (applicableRate is not null && input.ExchangeRate.HasValue)
        {
            var diff = (input.ExchangeRate.Value - applicableRate.Rate) / input.ExchangeRate.Value;
            if (diff <= 0m)
            {
                return new OrderConfirmationResult(
                    ConfirmationStatus.Confirmed,
                    Guid.NewGuid().ToString(),
                    input.ExchangeRate,
                    Math.Round(applicableRate.Rate * input.SourceAmount, 2, MidpointRounding.ToEven));
            }
        }
        return new OrderConfirmationResult(ConfirmationStatus.Deferred);
    }
}
