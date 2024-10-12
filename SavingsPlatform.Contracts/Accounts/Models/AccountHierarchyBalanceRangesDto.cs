namespace SavingsPlatform.Contracts.Accounts.Models;

public enum TransactionType
{
    Credit,
    Debit,
    InterestApplied
}

public record AccountBalanceRangeEntry(string AccountId, decimal MinTotalBalance, decimal MaxTotalBalance);

public class AccountHierarchyBalanceRangesDto
{
    public required string StreamId { get; set; }

    public IDictionary<string, AccountBalanceRangeEntry> BalanceRanges { get; init; } = new Dictionary<string, AccountBalanceRangeEntry>();

}