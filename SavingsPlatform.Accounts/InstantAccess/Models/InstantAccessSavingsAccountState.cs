using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Accounts.Aggregates.InstantAccess.Models
{
    public record InstantAccessSavingsAccountState : IAggregateStateEntry
    {
        public required string Key { get; init; } = string.Empty;
        public string? ExternalRef { get; init; }
        public DateTime? OpenedOn { get; set; }
        public DateTime? ActivatedOn { get; set; }
        public decimal InterestRate { get; set; }
        public decimal TotalBalance { get; set; }
        public decimal AccruedInterest {  get; set; }
        public Guid? LastTransactionId { get; set; }
        public ProcessFrequency InterestApplicationFrequency { get; set; } = ProcessFrequency.Weekly;
        public DateTime? InterestApplicationDueOn { get; set; }
        public string? PlatformId { get; init; }
        public bool HasUnpublishedEvents { get; set; } = false;
        public ICollection<object>? UnpublishedEvents { get; set; } = default;
        public AccountType Type { get; set; } = AccountType.SavingsAccount;
    } 
}
