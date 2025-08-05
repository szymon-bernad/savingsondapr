using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Contracts.Accounts.Models;

public record AccountCreationData
{
    public required string ExternalRef { get; init; }
    public string? AccountId { get; init; }
    public required string UserId { get; init; }
    public required Currency Currency { get; init; }
    public required AccountType AccountType { get; init; }
    public string? CurrentAccountId { get; init; } // For savings accounts, this is the reference to the current account used for deposits.
    public decimal InterestRate { get; init; } = 0m; // For savings accounts, this is the interest rate.
    public AccountCreationStatus Status { get; init; } = AccountCreationStatus.New;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

}
