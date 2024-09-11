using SavingsPlatform.Contracts.Accounts.Interfaces;

namespace SavingsPlatform.Contracts.Accounts.Events
{
    public record AccountInterestApplied(
        string Id,
        string ExternalRef,
        string AccountId,
        decimal AccruedInterest,
        decimal TotalBalance,
        decimal InterestRate,
        DateTime Timestamp,
        string EventType, 
        string? PlatformId) : IEvent;
}
