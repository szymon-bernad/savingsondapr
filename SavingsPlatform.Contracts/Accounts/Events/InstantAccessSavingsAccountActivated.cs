using SavingsPlatform.Contracts.Accounts.Interfaces;

namespace SavingsPlatform.Contracts.Accounts.Events
{
    public record InstantAccessSavingsAccountActivated(
        string Id,
        string ExternalRef,
        string AccountId,
        decimal Amount,
        decimal InterestRate,
        DateTime Timestamp,
        string EventType,
        string? PlatformId) : IEvent;
}
