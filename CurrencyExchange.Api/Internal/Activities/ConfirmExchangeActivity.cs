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
            ExchangeOrderType.GuaranteedRate => RunForGuaranteed(input),
            ExchangeOrderType.MarketRate     => RunForMarketRate(input),
            ExchangeOrderType.LimitOrder     => RunForLimitOrder(input),
            _ => throw new NotImplementedException(),
        };

        return result;
    }

    private async Task<OrderConfirmationResult> RunForGuaranteed(CurrencyExchangeOrder input)
    {
        var rates = await _exchangeRatesService.GetCurrentRatesAsync(input.SourceCurrency, input.TargetCurrency);
        var applicableRate = rates.ExchangeRates.FirstOrDefault(x => x.IsInRange(input.SourceAmount));

        if (applicableRate is not null && input.ExchangeRate.HasValue)
        {
            var diff = Math.Abs(applicableRate.Rate - input.ExchangeRate.Value) / input.ExchangeRate.Value;
            if (diff <= 0.1m)
            {
                return new OrderConfirmationResult(
                    ConfirmationStatus.Confirmed,
                    Guid.NewGuid().ToString(),
                    input.ExchangeRate,
                    input.ExchangeRate * input.SourceAmount);
            }
        }
        return new OrderConfirmationResult(ConfirmationStatus.Deferred);
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
                applicableRate.Rate * input.SourceAmount);
        }

        return new OrderConfirmationResult(ConfirmationStatus.Rejected);
    }

    private async Task<OrderConfirmationResult> RunForLimitOrder(CurrencyExchangeOrder input)
    {
        var rates = await _exchangeRatesService.GetCurrentRatesAsync(input.SourceCurrency, input.TargetCurrency);
        var applicableRate = rates.ExchangeRates.FirstOrDefault(x => x.IsInRange(input.SourceAmount));

        if (applicableRate is not null && input.ExchangeRate.HasValue)
        {
            var diff = (applicableRate.Rate - input.ExchangeRate.Value) / input.ExchangeRate.Value;
            if (diff <= 0.02m)
            {
                return new OrderConfirmationResult(
                    ConfirmationStatus.Confirmed,
                    Guid.NewGuid().ToString(),
                    input.ExchangeRate,
                    input.ExchangeRate.Value * input.SourceAmount);
            }
        }
        return new OrderConfirmationResult(ConfirmationStatus.Deferred);
    }
}
