using SavingsPlatform.Contracts.Accounts.Interfaces;

namespace SavingsPlatform.Contracts.Accounts.Events
{
    public record AccountInterestAccrued(
        string Id,
        string ExternalRef,
        string AccountId,
        decimal AccruedInterest,
        decimal TotalBalance,
        decimal InterestRate,
        DateTime Timestamp,
        string EvtType,
        string CurrentAccountId) : IEvent;
}
