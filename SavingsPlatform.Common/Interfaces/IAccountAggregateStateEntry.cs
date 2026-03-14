using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Common.Interfaces;

public interface IAccountAggregateStateEntry : IAggregateStateEntry
{
    public DateTime? OpenedOn { get; }
    public decimal TotalBalance { get; }
    public string CurrentAccountId { get; }
    public AccountType Type { get; }
}
