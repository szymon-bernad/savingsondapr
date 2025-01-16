using CurrencyExchange.Api.Internal.Models;
using Dapr.Workflow;
using SavingsPlatform.Common.Services;
using SavingsPlatform.Contracts.Accounts.Events;

namespace CurrencyExchange.Api.Internal.Activities;

public class FinalizeExchangeActivity(IEventPublishingService _pubService) : ActivityBase<CurrencyExchangeFinalState>
{
    public override Task<AccountActivityResult> RunAsync(WorkflowActivityContext context, CurrencyExchangeFinalState input)
        => RunWithRetries(async () =>
        {
            if(input.Result.Succeeded)
            {
                await _pubService.PublishEvents(
                    [
                        new CurrencyExchangeCompleted(
                            Guid.NewGuid().ToString(),
                            input.Order.DebtorExternalRef,
                            input.Order.BeneficiaryExternalRef,
                            input.Order.DebtorExternalRef,
                            input.Result.Receipt!.TransactionId,
                            input.Result.Receipt!.ExchangeRate,
                            input.Order.SourceAmount,
                            input.Order.SourceCurrency,
                            input.Order.TargetCurrency,
                            input.Order.OrderId,
                            input.Order.OrderType,
                            input.Result.Receipt!.TransactionDate,
                            typeof(CurrencyExchangeCompleted)!.Name)
                    ]);
            }
        });
}