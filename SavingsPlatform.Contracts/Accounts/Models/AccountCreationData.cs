using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Contracts.Accounts.Models;

public record AccountCreationData
{
    public required string ExternalRef { get; init; }
    public string? AccountId { get; init; }
    public required string UserId { get; init; }
    public required Currency Currency { get; init; }
    public required AccountType AccountType { get; init; }
    public AccountCreationStatus Status { get; init; } = AccountCreationStatus.New;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

}
