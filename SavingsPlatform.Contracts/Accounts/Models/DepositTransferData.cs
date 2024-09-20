using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Contracts.Accounts.Models
{
    public record DepositTransferData
    {
        public string DebtorAccountId { get; init; } = string.Empty;

        public string BeneficiaryAccountId { get; init; } = string.Empty;

        public decimal Amount { get; init; }

        public string TransactionId { get; init; } = string.Empty;

        public string? SettlementTransactionId { get; init; } = null;

        public string? SavingsTransactionId { get; init; } = null;

        public DepositTransferStatus Status { get; init; } = DepositTransferStatus.New;

        public TransferDirection Direction { get; init; } = TransferDirection.ToSavingsAccount;

        public bool IsFirstAttempt { get; init; } = true;

        public bool WaitForAccountCreation { get; init; } = false;
    }
}
