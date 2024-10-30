using Dapr.Workflow;
using SavingsPlatform.Contracts.Accounts.Models;
using SavingsPlatform.Contracts.Accounts.Requests;

namespace CurrencyExchange.Api.Internal
{
    public class CurrencyExchangeWorkflow : Workflow<CurrencyExchangeOrder, ExchangeReceipt>
    {
        public override async Task<ExchangeReceipt> RunAsync(WorkflowContext context, CurrencyExchangeOrder input)
        {
            var confirmationStep = await context.CallActivityAsync<OrderConfirmationResult>(nameof(ConfirmExchangeActivity), input);

            if (confirmationStep.IsConfirmed)
            {

            }

            throw new NotImplementedException();
        }
    }
}
