using CurrencyExchange.Api.Internal;
using CurrencyExchange.Api.Internal.Activities;
using Dapr.Workflow;
using NSubstitute;
using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Events;
using SavingsPlatform.Contracts.Accounts.Requests;

namespace CurrencyExchange.Api.UnitTests;
public class CurrencyExchangeWorkflowTests
{
    [Fact]
    public async Task RunAsync_WhenOrderConfirmed_ShouldContinueExchange()
    {
        // Arrange
        var beneficiaryExtRef = "beneficiary-ext-ref";
        var debtorExtRef = "debtor-ext-ref";

        var ctx = Substitute.For<WorkflowContext>();
        ctx.CallActivityAsync<OrderConfirmationResult>(nameof(ConfirmExchangeActivity), Arg.Any<CurrencyExchangeOrder>())
            .Returns(new OrderConfirmationResult(ConfirmationStatus.Confirmed, "qwertyzwsx", 1.0m, 1000));

        ctx.CallActivityAsync<AccountActivityResult>(nameof(DebitAccountActivity), Arg.Any<DebitAccount>())
            .Returns(new AccountActivityResult(true));
        ctx.CallActivityAsync<AccountActivityResult>(nameof(CreditAccountActivity), Arg.Any<CreditAccount>())
            .Returns(new AccountActivityResult(true));

        ctx.WaitForExternalEventAsync<AccountDebited>("accountdebited")
            .ReturnsForAnyArgs(new AccountDebited (
                Id: "testId",
                AccountId: debtorExtRef,
                AccountType: AccountType.CurrentAccount,
                Amount: 1000m,
                CurrentAccountId: debtorExtRef,
                EvtType: "accountdebited",
                ExternalRef: "extref",
                Timestamp: DateTime.UtcNow,
                TotalBalance: 0m,
                OperationId: "operationId",
                TransferId: "trasnsferId"));
        ctx.WaitForExternalEventAsync<AccountCredited>("accountcredited")
                .ReturnsForAnyArgs(new AccountCredited(
                    Id: "testId",
                    AccountId: "accId",
                    AccountType: AccountType.CurrentAccount,
                    Amount: 1000m,
                    CurrentAccountId: beneficiaryExtRef,
                    EvtType: "accountcredited",
                    ExternalRef: beneficiaryExtRef,
                    Timestamp: DateTime.UtcNow,
                    TotalBalance: 0m,
                    OperationId: "operationId",
                    TransferId: "trasnsferId"));

        var sut = new CurrencyExchangeWorkflow();

        // Act
        var result = await sut.RunAsync(ctx, 
            new CurrencyExchangeOrder(
                DebtorExternalRef: debtorExtRef,
                BeneficiaryExternalRef: beneficiaryExtRef,
                OrderId: "orderId",
                OrderType: ExchangeOrderType.MarketRate,
                SourceAmount: 1000m,
                SourceCurrency: Currency.USD,
                TargetCurrency: Currency.EUR,
                ExchangeRate: null
                ));

        //Assert

        Assert.True(result.Succeeded);
        Assert.Equal(1.0m, result.Receipt!.ExchangeRate);
        Assert.Equal(1000m, result.Receipt!.TargetAmount);
    }
}