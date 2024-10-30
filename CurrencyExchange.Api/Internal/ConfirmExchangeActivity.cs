using Dapr.Workflow;
using SavingsPlatform.Contracts.Accounts.Requests;

namespace CurrencyExchange.Api.Internal;

public class ConfirmExchangeActivity : WorkflowActivity<CurrencyExchangeOrder, OrderConfirmationResult>
{
    public override Task<OrderConfirmationResult> RunAsync(WorkflowActivityContext context, CurrencyExchangeOrder input)
    {
        throw new NotImplementedException();
    }
}
