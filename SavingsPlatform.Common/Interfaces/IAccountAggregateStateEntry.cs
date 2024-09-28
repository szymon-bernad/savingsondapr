using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Common.Interfaces;

public interface IAccountAggregateStateEntry : IAggregateStateEntry
{
    public string Key { get; }
    public string ExternalRef { get; }
    public DateTime? OpenedOn { get; }
    public decimal TotalBalance { get; }
    public string CurrentAccountId { get; }
    public bool HasUnpublishedEvents { get; }
    public ICollection<object>? UnpublishedEvents { get; }
    public AccountType Type { get; }
}
