using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Contracts.Accounts.Models
{
    public record DepositTransferData
    {
        public required string DebtorAccountId { get; init; }

        public required string BeneficiaryAccountId { get; init; }

        public required decimal Amount { get; init; }

        public required string TransferId { get; init; }

        public DepositTransferStatus Status { get; init; } = DepositTransferStatus.New;

        public required TransferType Direction { get; init; }

        public bool IsFirstAttempt { get; init; } = true;
    }
}
