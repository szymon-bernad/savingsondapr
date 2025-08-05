using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts;

namespace SavingsPlatform.Accounts.AccountHolders;

public record AccountHolderState : IAggregateStateEntry
{
    public required string Key { get; init; }

    public string? Username { get; init; } = default;

    public DateTime? AddedOn { get; init; }

    public required string ExternalRef { get; init; }

    public List<AccountInfo> Accounts { get; init; } = [];

    public bool HasUnpublishedEvents { get; set; } = false;

    public ICollection<object>? UnpublishedEvents { get; set; } = default;

}
