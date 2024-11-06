using CurrencyExchange.Api.Internal.Activities;
using Dapr.Workflow;
using SavingsPlatform.Contracts.Accounts.Events;
using SavingsPlatform.Contracts.Accounts.Models;
using SavingsPlatform.Contracts.Accounts.Requests;

namespace CurrencyExchange.Api.Internal;

public class CurrencyExchangeWorkflow : Workflow<CurrencyExchangeOrder, ExchangeResult>
{
    public override async Task<ExchangeResult> RunAsync(WorkflowContext context, CurrencyExchangeOrder input)
    {
        var confirmationStep = await context.CallActivityAsync<OrderConfirmationResult>(nameof(ConfirmExchangeActivity), input);

        while (confirmationStep.Status == ConfirmationStatus.Deferred)
        {
            await context.CreateTimer(TimeSpan.FromMinutes(3));
            confirmationStep = await context.CallActivityAsync<OrderConfirmationResult>(nameof(ConfirmExchangeActivity), input);
        }

        ExchangeResult? result = null;

        if (confirmationStep.Status != ConfirmationStatus.Confirmed ||
            confirmationStep.EffectiveRate is null ||
            confirmationStep.TargetAmount is null)
        {
            result = new ExchangeResult(false, $"Exchange Order has been rejected", null);
        }
        else
        {
            var debitStep = await context.CallActivityAsync<bool>(
                nameof(DebitAccountActivity),
                new DebitAccount(
                    input.DebtorExternalRef,
                    input.SourceAmount,
                    DateTime.UtcNow,
                    $"{context.InstanceId}^1",
                    null));

            AccountDebited? ev = null;
            try
            {
                ev = await context.WaitForExternalEventAsync<AccountDebited>("accountdebited", timeout: TimeSpan.FromMinutes(30));
            }
            catch (TaskCanceledException ex)
            {
                result = new ExchangeResult(false, $"Exchange Order has been rejected: {ex.Message}", null);
            }

            if (result is null)
            {
                var creditStep = await context.CallActivityAsync<bool>(
                    nameof(CreditAccountActivity),
                    new CreditAccount(
                        input.BeneficiaryExternalRef,
                        confirmationStep.TargetAmount.Value,
                        DateTime.UtcNow,
                        $"{context.InstanceId}^2",
                        null));

                var ev2 = await context.WaitForExternalEventAsync<AccountCredited>("accountcredited");
                result = new ExchangeResult(true,
                                $"Exchange Order has been fulfilled",
                                new ExchangeReceipt(
                                    confirmationStep.TargetAmount.Value,
                                    confirmationStep.EffectiveRate.Value,
                                    DateTime.UtcNow,
                                    context.InstanceId,
                                    input.DebtorExternalRef,
                                    input.BeneficiaryExternalRef,
                                    input.SourceCurrency,
                                    input.TargetCurrency));
            }
        }
        return result;
    }
}