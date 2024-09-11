using System;

namespace SavingsPlatform.Contracts.Accounts.Enums
{
    public static class DepositRequestDetailsKeys
    {
        public const string TransferDirection = nameof(TransferDirection);

        public const string TransferAmount = nameof(TransferAmount);

        public const string TransferCurrency = nameof(TransferCurrency);

        public const string InterestRate = nameof(InterestRate);
        
        public const string SettlementAccountRef = nameof(SettlementAccountRef);

        public const string PlatformId = nameof(PlatformId);
    }
}
