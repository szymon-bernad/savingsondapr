using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Accounts.Aggregates.InstantAccess.Models
{
    public record InstantAccessSavingsAccountDto(
        string Id,
        string ExternalRef,
        DateTime? OpenedOn,
        DateTime? ActivatedOn,
        decimal InterestRate,
        decimal TotalBalance,
        decimal AccruedInterest,
        string CurrentAccountId,
        ProcessFrequency InterestApplicationFrequency = ProcessFrequency.Weekly,
        DateTime? InterestApplicationDueOn = null,
        DateTime? InterestAccruedOn = null,
        AccountType Type = AccountType.SavingsAccount);
}
