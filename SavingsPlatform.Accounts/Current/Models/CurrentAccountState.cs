using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Accounts.Current.Models;

public record CurrentAccountState : IAccountAggregateStateEntry
{
    public required string Key { get; init; } = string.Empty;
    public required string ExternalRef { get; init; } = string.Empty;
    public DateTime? OpenedOn { get; set; }
    public decimal TotalBalance { get; set; }
    public required string PlatformId { get; init; }
    public bool HasUnpublishedEvents { get; set; } = false;
    public ICollection<object>? UnpublishedEvents { get; set; } = default;
    public AccountType Type { get; set; } = AccountType.CurrentAccount;
}
