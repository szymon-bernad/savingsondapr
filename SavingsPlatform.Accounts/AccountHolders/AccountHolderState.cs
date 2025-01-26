using SavingsPlatform.Common.Interfaces;

namespace SavingsPlatform.Accounts.AccountHolders;

public record AccountHolderState : IAggregateStateEntry
{
    public required string Key { get; init; }

    public string? Username { get; init; } = default;

    public DateTime? AddedOn { get; init; }

    public required string ExternalRef { get; init; }

    public ICollection<string> AccountIds { get; init; } = new List<string>();

    public bool HasUnpublishedEvents { get; set; } = false;

    public ICollection<object>? UnpublishedEvents { get; set; } = default;

}
