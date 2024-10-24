using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Accounts.Aggregates.InstantAccess.Models
{
    public record InstantAccessSavingsAccountDto(
        DateTime? OpenedOn,
        DateTime? ActivatedOn,
        decimal InterestRate,
        decimal TotalBalance,
        decimal AccruedInterest,
        string CurrentAccountId,
        ProcessFrequency InterestApplicationFrequency = ProcessFrequency.Weekly,
        DateTime? InterestApplicationDueOn = null,
        DateTime? InterestAccruedOn = null,
        Currency Currency = Currency.EUR,
        AccountType Type = AccountType.SavingsAccount);
}
