using CurrencyExchange.Api.Internal.Activities;
using Dapr.Workflow;
using SavingsPlatform.Contracts.Accounts.Events;
using SavingsPlatform.Contracts.Accounts.Models;
using SavingsPlatform.Contracts.Accounts.Requests;

namespace CurrencyExchange.Api.Internal
{
    public class CurrencyExchangeWorkflow : Workflow<CurrencyExchangeOrder, ExchangeResult>
    {
        public override async Task<ExchangeResult> RunAsync(WorkflowContext context, CurrencyExchangeOrder input)
        {
            var confirmationStep = await context.CallActivityAsync<OrderConfirmationResult>(nameof(ConfirmExchangeActivity), input);

            while(confirmationStep.Status == ConfirmationStatus.Deferred)
            {
                await context.CreateTimer(TimeSpan.FromMinutes(10));
                confirmationStep = await context.CallActivityAsync<OrderConfirmationResult>(nameof(ConfirmExchangeActivity), input);
            }

            if (confirmationStep.Status == ConfirmationStatus.Confirmed)
            {
                var debitId = context.InstanceId + "-1";

                var debitStep = await context.CallActivityAsync<bool>(
                    nameof(DebitAccountActivity),
                    new DebitAccount(
                        input.DebtorExternalRef,
                        input.SourceAmount,
                        DateTime.UtcNow,
                        debitId, 
                        context.InstanceId));
                var ev = await context.WaitForExternalEventAsync<AccountDebited>("AccountDebited");

                if (debitId.Equals(ev.TransferId, StringComparison.Ordinal) && ev.Amount == input.SourceAmount)
                {
                    var creditId = context.InstanceId + "-2";
                    var creditStep = await context.CallActivityAsync<bool>(
                        nameof(CreditAccountActivity),
                        new CreditAccount(
                            input.BeneficiaryExternalRef, 
                            confirmationStep.TargetAmount!.Value, 
                            DateTime.UtcNow, 
                            creditId,
                            context.InstanceId));
                    var ev2 = await context.WaitForExternalEventAsync<AccountCredited>("AccountCredited");

                    if (creditId.Equals(ev2.TransferId, StringComparison.Ordinal) && ev2.Amount == confirmationStep.TargetAmount!.Value)
                    {
                        return new ExchangeResult(true,
                            $"Exchange Order has been fulfilled.",
                            new ExchangeReceipt(
                                confirmationStep.TargetAmount!.Value,
                                confirmationStep.EffectiveRate!.Value,
                                DateTime.UtcNow,
                                confirmationStep.TransactionId,
                                input.DebtorExternalRef,
                                input.BeneficiaryExternalRef,
                                input.SourceCurrency,
                                input.TargetCurrency));
                    }
                }

                return new ExchangeResult(true, 
                    $"Exchange Order has been fulfilled.",
                    new ExchangeReceipt(
                        confirmationStep.TargetAmount!.Value,
                        confirmationStep.EffectiveRate!.Value,
                        DateTime.UtcNow,
                        confirmationStep.TransactionId,
                        input.DebtorExternalRef,
                        input.BeneficiaryExternalRef,
                        input.SourceCurrency,
                        input.TargetCurrency));
            }
            return new ExchangeResult(false, $"Exchange Order has been rejected.", null);
        }
    }
}
