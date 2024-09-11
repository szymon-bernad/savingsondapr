using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Interfaces;

namespace SavingsPlatform.Contracts.Accounts.Events
{
    public record AccountCreated : IEvent
    {
        public required string Id { get; set; }

        public required string ExternalRef { get; set; }

        public string? SettlementAccountRef { get; set; }

        public string PlatformId { get; set; } = string.Empty;

        public required string AccountId { get; set; }

        public AccountType AccountType { get; set; }

        public DateTime Timestamp { get; set; }

        public required string EventType { get; set; }

        public string? TransferId { get; set; }
    }
}
