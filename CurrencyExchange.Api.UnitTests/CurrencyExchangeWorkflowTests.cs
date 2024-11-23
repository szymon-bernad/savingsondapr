using AutoFixture.Xunit2;
using CurrencyExchange.Api.Internal;
using CurrencyExchange.Api.Internal.Activities;
using Dapr.Workflow;
using NSubstitute;
using SavingsPlatform.Contracts.Accounts.Events;
using SavingsPlatform.Contracts.Accounts.Requests;

namespace CurrencyExchange.Api.UnitTests;
public class CurrencyExchangeWorkflowTests
{
    [Theory]
    [InlineAutoData]
    public async Task RunAsync_WhenOrderConfirmed_ShouldContinueExchange(
        string beneficiaryExtRef,
        string debtorExtRef,
        AccountDebited debitedEvent,
        AccountCredited creditedEvent,
        CurrencyExchangeOrder exchangeOrder)
    {
        // Arrange
        debitedEvent = debitedEvent with { ExternalRef = debtorExtRef };
        creditedEvent = creditedEvent with { ExternalRef = beneficiaryExtRef };
        exchangeOrder = exchangeOrder with { DebtorExternalRef = debtorExtRef, BeneficiaryExternalRef = beneficiaryExtRef };
        var ctx = Substitute.For<WorkflowContext>();
        SetupWorkflowContext_HappyPath(ctx, debtorExtRef, beneficiaryExtRef, debitedEvent, creditedEvent, 0.755m, 1010m);

        var sut = new CurrencyExchangeWorkflow();

        // Act
        var result = await sut.RunAsync(ctx, exchangeOrder);

        //Assert
        Assert.True(result.Succeeded);
        Assert.Equal(0.755m, result.Receipt!.ExchangeRate);
        Assert.Equal(1010m, result.Receipt!.TargetAmount);
    }

    [Theory]
    [InlineAutoData]
    public async Task RunAsync_WhenOrderConfirmed_CreditStepFailed_ShouldContinueWithRevert(
        string beneficiaryExtRef,
        string debtorExtRef,
        AccountDebited debitedEvent,
        AccountCredited creditedEvent,
        CurrencyExchangeOrder exchangeOrder)
    {
        // Arrange
        debitedEvent = debitedEvent with { ExternalRef = debtorExtRef };
        creditedEvent = creditedEvent with { ExternalRef = debtorExtRef };
        exchangeOrder = exchangeOrder with { DebtorExternalRef = debtorExtRef, BeneficiaryExternalRef = beneficiaryExtRef };
        var ctx = Substitute.For<WorkflowContext>();
        SetupWorkflowContext_FailingCredit(ctx, debtorExtRef, beneficiaryExtRef, debitedEvent, creditedEvent, 1.01m, 1010m);

        var sut = new CurrencyExchangeWorkflow();

        // Act
        var result = await sut.RunAsync(ctx, exchangeOrder);

        //Assert
        Assert.False(result.Succeeded);

        await ctx.Received(1).CallActivityAsync<AccountActivityResult>(
            nameof(DebitAccountActivity),
             Arg.Is<DebitAccount>(c => debtorExtRef.Equals(c.ExternalRef, StringComparison.Ordinal)));

        await ctx.Received(1).CallActivityAsync<AccountActivityResult>(
            nameof(CreditAccountActivity),
            Arg.Is<CreditAccount>(c => beneficiaryExtRef.Equals(c.ExternalRef, StringComparison.Ordinal)));
        await ctx.Received(1).CallActivityAsync<AccountActivityResult>(
            nameof(CreditAccountActivity),
            Arg.Is<CreditAccount>(c => debtorExtRef.Equals(c.ExternalRef, StringComparison.Ordinal)));
    }

    [Theory]
    [InlineAutoData]
    public async Task RunAsync_WhenOrderDeferredThenRejected_ShouldTerminate(
    string beneficiaryExtRef,
    string debtorExtRef,
    AccountDebited debitedEvent,
    AccountCredited creditedEvent,
    CurrencyExchangeOrder exchangeOrder)
    {
        // Arrange
        debitedEvent = debitedEvent with { ExternalRef = debtorExtRef };
        creditedEvent = creditedEvent with { ExternalRef = beneficiaryExtRef };
        exchangeOrder = exchangeOrder with { DebtorExternalRef = debtorExtRef, BeneficiaryExternalRef = beneficiaryExtRef };
        var ctx = Substitute.For<WorkflowContext>();
        
        SetupWorkflowContext_HappyPath(ctx, debtorExtRef, beneficiaryExtRef, debitedEvent, creditedEvent, 0.755m, 1010m);
        ctx.CallActivityAsync<OrderConfirmationResult>(
            nameof(ConfirmExchangeActivity),
            Arg.Is<CurrencyExchangeOrder>(o => o.OrderId == exchangeOrder.OrderId))
            .Returns(
                new OrderConfirmationResult(ConfirmationStatus.Deferred),
                new OrderConfirmationResult(ConfirmationStatus.Deferred),
                new OrderConfirmationResult(ConfirmationStatus.Rejected));

        var sut = new CurrencyExchangeWorkflow();

        // Act
        var result = await sut.RunAsync(ctx, exchangeOrder);

        //Assert
        Assert.False(result.Succeeded);
        await ctx.Received(3).CallActivityAsync<OrderConfirmationResult>(
            nameof(ConfirmExchangeActivity),
            Arg.Is<CurrencyExchangeOrder>(o => o.OrderId == exchangeOrder.OrderId));
        await ctx.DidNotReceive().CallActivityAsync<AccountActivityResult>(
            nameof(DebitAccountActivity),
             Arg.Is<DebitAccount>(c => debtorExtRef.Equals(c.ExternalRef, StringComparison.Ordinal)));

    }

    private void SetupWorkflowContext_HappyPath(
        WorkflowContext ctx,
        string debtorExtRef,
        string beneficiaryExtRef,
        AccountDebited debitedEvt,
        AccountCredited creditedEvt,
        decimal exchangeRate,
        decimal targetAmount)
    {
        ctx.CallActivityAsync<OrderConfirmationResult>(nameof(ConfirmExchangeActivity), Arg.Any<CurrencyExchangeOrder>())
            .Returns(new OrderConfirmationResult(ConfirmationStatus.Confirmed, "qwertyzwsx", exchangeRate, targetAmount));

        ctx.CallActivityAsync<AccountActivityResult>(
            nameof(DebitAccountActivity),
            Arg.Is<DebitAccount>(
                c => c.ExternalRef.Equals(debtorExtRef, StringComparison.Ordinal)))
            .Returns(new AccountActivityResult(true));
        ctx.CallActivityAsync<AccountActivityResult>(
            nameof(CreditAccountActivity),
            Arg.Is<CreditAccount>(
                c => c.ExternalRef.Equals(beneficiaryExtRef, StringComparison.Ordinal)))
            .Returns(new AccountActivityResult(true));

        ctx.WaitForExternalEventAsync<AccountDebited>("accountdebited")
            .ReturnsForAnyArgs(debitedEvt);
        ctx.WaitForExternalEventAsync<AccountCredited>("accountcredited")
            .ReturnsForAnyArgs(creditedEvt);
    }

    private void SetupWorkflowContext_FailingCredit(
        WorkflowContext ctx,
        string debtorExtRef,
        string beneficiaryExtRef,
        AccountDebited debitedEvt,
        AccountCredited creditedEvt,
        decimal exchangeRate,
        decimal targetAmount)
    {
        ctx.CallActivityAsync<OrderConfirmationResult>(nameof(ConfirmExchangeActivity), Arg.Any<CurrencyExchangeOrder>())
            .Returns(new OrderConfirmationResult(ConfirmationStatus.Confirmed, "qwertyzwsx", exchangeRate, targetAmount));

        ctx.CallActivityAsync<AccountActivityResult>(nameof(DebitAccountActivity), Arg.Any<DebitAccount>())
            .Returns(new AccountActivityResult(true));
        ctx.CallActivityAsync<AccountActivityResult>(
            nameof(CreditAccountActivity),
            Arg.Is<CreditAccount>(
                c => c.ExternalRef.Equals(debtorExtRef, StringComparison.Ordinal)))
            .Returns(new AccountActivityResult(true));
        ctx.CallActivityAsync<AccountActivityResult>(
            nameof(CreditAccountActivity),
            Arg.Is<CreditAccount>(
                c => c.ExternalRef.Equals(beneficiaryExtRef, StringComparison.Ordinal)))
            .Returns(new AccountActivityResult(false, "Critical error", false));

        ctx.WaitForExternalEventAsync<AccountDebited>("accountdebited")
            .ReturnsForAnyArgs(debitedEvt);
        ctx.WaitForExternalEventAsync<AccountCredited>("accountcredited")
            .ReturnsForAnyArgs(creditedEvt);
    }
}