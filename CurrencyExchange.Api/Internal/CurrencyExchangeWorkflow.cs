﻿using CurrencyExchange.Api.Internal.Activities;
using CurrencyExchange.Api.Internal.Models;
using Dapr.Workflow;
using SavingsPlatform.Contracts.Accounts.Events;
using SavingsPlatform.Contracts.Accounts.Models;
using SavingsPlatform.Contracts.Accounts.Requests;
using static FastExpressionCompiler.ExpressionCompiler;

namespace CurrencyExchange.Api.Internal;

public class CurrencyExchangeWorkflow : Workflow<CurrencyExchangeOrder, ExchangeResult>
{
    public override async Task<ExchangeResult> RunAsync(WorkflowContext context, CurrencyExchangeOrder input)
    {
        var confirmationStep = await context.CallActivityAsync<OrderConfirmationResult>(nameof(ConfirmExchangeActivity), input);

        while (confirmationStep.Status == ConfirmationStatus.Deferred)
        {
            await context.CreateTimer(TimeSpan.FromMinutes(1));
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
            result = await RunExchangeTransfer(context, input, confirmationStep);
            result ??= new ExchangeResult(false, $"Exchange Order execution failed.", null);
        }

        return result;
    }

    private async Task<ExchangeResult?> RunExchangeTransfer(
        WorkflowContext context,
        CurrencyExchangeOrder input,
        OrderConfirmationResult confirmationStep)
    {
        ExchangeResult? result = null;
        var debitStep = await context.CallActivityAsync<AccountActivityResult>(
                                nameof(DebitAccountActivity),
                                new DebitAccount(
                                    input.DebtorExternalRef,
                                    input.SourceAmount,
                                    DateTime.UtcNow,
                                    $"{context.InstanceId}^1",
                                    null));
        if (!debitStep.Succeeded)
        {
            result = new ExchangeResult(false, $"Exchange Order execution failed", null);
        }

        if (result is null)
        {
            var event1 = await context.WaitForExternalEventAsync<AccountDebited>("accountdebited");

            var creditStep = await context.CallActivityAsync<AccountActivityResult>(
                    nameof(CreditAccountActivity),
                    new CreditAccount(
                        input.BeneficiaryExternalRef,
                        confirmationStep.TargetAmount!.Value,
                        DateTime.UtcNow,
                        $"{context.InstanceId}^2",
                        null));

            AccountActivityResult? revertStep = null;
            if (!creditStep.Succeeded)
            {
                revertStep = await context.CallActivityAsync<AccountActivityResult>(
                        nameof(CreditAccountActivity),
                        new CreditAccount(
                            input.DebtorExternalRef,
                            input.SourceAmount,
                            DateTime.UtcNow,
                            $"{context.InstanceId}^revert",
                            null));
            }

            if (creditStep.Succeeded || (revertStep?.Succeeded ?? false))
            {
                var event2 = await context.WaitForExternalEventAsync<AccountCredited>("accountcredited");
                if (creditStep.Succeeded &&
                    input.BeneficiaryExternalRef.Equals(event2.ExternalRef, StringComparison.Ordinal))
                {
                    result = new ExchangeResult(true,
                        $"Exchange Order has been fulfilled",
                        new ExchangeReceipt(
                            confirmationStep.TargetAmount.Value,
                            confirmationStep.EffectiveRate!.Value,
                            DateTime.UtcNow,
                            context.InstanceId,
                            input.DebtorExternalRef,
                            input.BeneficiaryExternalRef,
                            input.SourceCurrency,
                            input.TargetCurrency));

                    var finalStep = await context.CallActivityAsync<AccountActivityResult>(
                        nameof(FinalizeExchangeActivity),
                        new CurrencyExchangeFinalState { Order = input, Result = result });
                }
            }

            if (revertStep is not null && !revertStep.Succeeded)
            {
                result = new ExchangeResult(false, $"Exchange Order execution failed. Compensation Step might be required.", null);
            }
        }

        return result;
    }
}