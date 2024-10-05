using Dapr.Actors.Runtime;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Aggregates.InstantAccess;
using SavingsPlatform.Accounts.Current.Models;
using SavingsPlatform.Accounts.Current;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Common.Services;
using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Models;
using Xunit;
using SavingsPlatform.Accounts.Actors.Services;
using NSubstitute;
using SavingsPlatform.Contracts.Accounts.Commands;
using static FastExpressionCompiler.ExpressionCompiler;

namespace SavingsPlatform.Accounts.UnitTests;

public class DepositTransferServiceTests
{
    [Fact]
    public async Task InitiateTransferAsync_WhenCalledWithValidData_ShouldStartTransfer()
    {
        // Arrange

        var depositTransfer = new DepositTransferData
        {
            TransferId = "transferId",
            DebtorAccountId = Guid.NewGuid().ToString(),
            BeneficiaryAccountId = Guid.NewGuid().ToString(),
            Amount = 500,
            Direction = TransferType.CurrentToSavings,
            IsFirstAttempt = true,
            Status = DepositTransferStatus.New
        };

        var (actorStateMngrMock, currentAccountQueryHandler, iasaQueryHandler, eventPublishingServiceMock) = GetMockSetup(depositTransfer, 1500);

        var sut = new DepositTransferService(actorStateMngrMock, currentAccountQueryHandler, iasaQueryHandler, eventPublishingServiceMock);

        // Act
        var result = await sut.InitiateTransferAsync(depositTransfer);


        // Assert
        Assert.Null(result);
        await eventPublishingServiceMock.Received(1).PublishCommand(Arg.Any<DebitAccountCommand>());
        await actorStateMngrMock.DidNotReceive().GetStateAsync<DepositTransferData>(DepositTransferService.DepositTransferState);
    }

    [Fact]
    public async Task InitiateTransferAsync_WhenCalledWithStatusNotNew_ShouldReturnUnregisterString()
    {
        // Arrange

        var depositTransfer = new DepositTransferData
        {
            TransferId = "transferId",
            DebtorAccountId = Guid.NewGuid().ToString(),
            BeneficiaryAccountId = Guid.NewGuid().ToString(),
            Amount = 500,
            Direction = TransferType.CurrentToSavings,
            IsFirstAttempt = true,
            Status = DepositTransferStatus.DebtorDebited
        };

        var (actorStateMngrMock, currentAccountQueryHandler, iasaQueryHandler, eventPublishingServiceMock) = GetMockSetup(depositTransfer, 1500);

        var sut = new DepositTransferService(actorStateMngrMock, currentAccountQueryHandler, iasaQueryHandler, eventPublishingServiceMock);

        // Act
        var result = await sut.InitiateTransferAsync(depositTransfer);

        // Assert
        Assert.Equal(DepositTransferService.TransferAttemptUnregister, result);
        await eventPublishingServiceMock.DidNotReceive().PublishCommand(Arg.Any<DebitAccountCommand>());
        await actorStateMngrMock.DidNotReceive().GetStateAsync<DepositTransferData>(DepositTransferService.DepositTransferState);
    }

    [Theory]
    [InlineData(TransferType.CurrentToSavings)]
    [InlineData(TransferType.SavingsToCurrent)]
    public async Task InitiateTransferAsync_WhenCalledWithInsufficientDebtorBalance_ShouldNotProceedWithDebitCmd(
        TransferType transferDir)
    {
        // Arrange
        var depositTransfer = new DepositTransferData
        {
            TransferId = "transferId",
            DebtorAccountId = Guid.NewGuid().ToString(),
            BeneficiaryAccountId = Guid.NewGuid().ToString(),
            Amount = 2500,
            Direction = transferDir,
            IsFirstAttempt = true,
            Status = DepositTransferStatus.New
        };

        var (actorStateMngrMock, currentAccountQueryHandler, iasaQueryHandler, eventPublishingServiceMock) = GetMockSetup(depositTransfer, 1500);
        var sut = new DepositTransferService(actorStateMngrMock, currentAccountQueryHandler, iasaQueryHandler, eventPublishingServiceMock);
        
        // Act & Assert
        if (transferDir == TransferType.CurrentToSavings)
        {
            var result = await sut.InitiateTransferAsync(depositTransfer);
            Assert.Equal(DepositTransferService.TransferAttemptRegister, result);
        }
        else if (transferDir == TransferType.SavingsToCurrent)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await sut.InitiateTransferAsync(depositTransfer));
        }

        await eventPublishingServiceMock.DidNotReceive().PublishCommand(Arg.Any<DebitAccountCommand>());
        await actorStateMngrMock.DidNotReceive().GetStateAsync<DepositTransferData>(DepositTransferService.DepositTransferState);
    }

    private (IActorStateManager,
        IStateEntryQueryHandler<CurrentAccountState>,
        IStateEntryQueryHandler<InstantAccessSavingsAccountState>,
        IEventPublishingService)
        GetMockSetup(DepositTransferData dtd, decimal debtorBalance)
    {
        var actorStateMngrMock = Substitute.For<IActorStateManager>();

        IStateEntryQueryHandler<CurrentAccountState> currentAccountQueryHandler = Substitute.For<IStateEntryQueryHandler<CurrentAccountState>>();
        IStateEntryQueryHandler<InstantAccessSavingsAccountState> iasaQueryHandler = Substitute.For<IStateEntryQueryHandler<InstantAccessSavingsAccountState>>();

        var eventPublishingServiceMock = Substitute.For<IEventPublishingService>();

        actorStateMngrMock.GetStateAsync<DepositTransferData>(DepositTransferService.DepositTransferState)
            .Returns(dtd);

        currentAccountQueryHandler.GetAccountAsync(dtd.DebtorAccountId)
            .Returns(
                new CurrentAccountState
                {
                    ExternalRef = "current-ext-ref",
                    Key = dtd.DebtorAccountId,
                    OpenedOn = DateTime.UtcNow.AddDays(-5),
                    TotalBalance = debtorBalance,
                });

        iasaQueryHandler.GetAccountAsync(dtd.DebtorAccountId)
            .Returns(
                new InstantAccessSavingsAccountState
                {
                    ExternalRef = "iasa-ext-ref",
                    Key = dtd.DebtorAccountId,
                    CurrentAccountId = dtd.BeneficiaryAccountId,
                    InterestRate = 2.5m,
                    OpenedOn = DateTime.UtcNow.AddDays(-5),
                    TotalBalance = debtorBalance,
                });

        return (actorStateMngrMock, currentAccountQueryHandler, iasaQueryHandler, eventPublishingServiceMock);
    }
}
