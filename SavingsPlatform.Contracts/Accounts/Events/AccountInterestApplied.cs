using SavingsPlatform.Contracts.Accounts.Interfaces;

namespace SavingsPlatform.Contracts.Accounts.Events
{
    public record AccountInterestApplied(
        string Id,
        string ExternalRef,
        string AccountId,
        decimal TotalBalance,
        decimal Amount,
        decimal InterestRate,
        DateTime Timestamp,
        string EvtType, 
        string CurrentAccountId) : IEvent;
}
