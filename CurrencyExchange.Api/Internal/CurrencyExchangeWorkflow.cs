using CurrencyExchange.Api.Internal.Activities;
using Dapr.Workflow;
using SavingsPlatform.Contracts.Accounts.Events;
using SavingsPlatform.Contracts.Accounts.Models;
using SavingsPlatform.Contracts.Accounts.Requests;

namespace CurrencyExchange.Api.Internal;

public class CurrencyExchangeWorkflow : Workflow<CurrencyExchangeOrder, ExchangeResult>
{
    private const int ActivityRetriesCount = 3;

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
            var debitStep = await RunWithRetries(context, async ctx =>
                    await ctx.CallActivityAsync<AccountActivityResult>(
                        nameof(DebitAccountActivity),
                        new DebitAccount(
                            input.DebtorExternalRef,
                            input.SourceAmount,
                            DateTime.UtcNow,
                            $"{ctx.InstanceId}^1",
                            null)));
            if(!debitStep.Succeeded)
            {
                result = new ExchangeResult(false, $"Exchange Order execution failed", null);
            }

            if (result is null)
            {
                AccountDebited? event1 = null;
                try
                {
                    event1 = await context.WaitForExternalEventAsync<AccountDebited>("accountdebited");
                }
                catch (TaskCanceledException ex)
                {
                    result = new ExchangeResult(false, $"Exchange Order has been rejected: {ex.Message}", null);
                }

                if (result is null)
                {
                    AccountActivityResult? revertStep = null;
                    var creditStep = await RunWithRetries(context, async ctx =>
                        await ctx.CallActivityAsync<AccountActivityResult>(
                            nameof(CreditAccountActivity),
                            new CreditAccount(
                                input.BeneficiaryExternalRef,
                                confirmationStep.TargetAmount.Value,
                                DateTime.UtcNow,
                                $"{context.InstanceId}^2",
                                null)));
                    if (!creditStep.Succeeded)
                    {
                        revertStep = await RunWithRetries(context, async ctx =>
                            await ctx.CallActivityAsync<AccountActivityResult>(
                                nameof(CreditAccountActivity),
                                new CreditAccount(
                                    input.DebtorExternalRef,
                                    input.SourceAmount,
                                    DateTime.UtcNow,
                                    $"{ctx.InstanceId}^revert",
                                    null)));
                    }

                    if (creditStep.Succeeded || (revertStep?.Succeeded ?? false))
                    {
                        var event2 = await context.WaitForExternalEventAsync<AccountCredited>("accountcredited");
                        if (creditStep.Succeeded && input.BeneficiaryExternalRef.Equals(event2.ExternalRef, StringComparison.Ordinal))
                        {
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

                    if (revertStep is not null && !revertStep.Succeeded)
                    {
                        result = new ExchangeResult(false, $"Exchange Order execution failed - revert might be required", null);
                    }

                    result ??= new ExchangeResult(false, $"Exchange Order execution failed", null);
                }
            }
        }
        return result;
    }

    private async Task<AccountActivityResult> RunWithRetries(WorkflowContext context, Func<WorkflowContext, Task<AccountActivityResult>> funcToRun)
    {
        var attempt = 0;
        var result = await funcToRun(context);

        while (!result.Succeeded && result.IsRetriable && attempt < ActivityRetriesCount)
        {
            ++attempt;
            result = await funcToRun(context);
        }

        return result;
    }
}