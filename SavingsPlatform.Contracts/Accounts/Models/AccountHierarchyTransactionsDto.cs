namespace SavingsPlatform.Contracts.Accounts.Models;

public enum TransactionType
{
    Credit,
    Debit,
    InterestApplied
}

public record TransactionEntry(string TransactionId, TransactionType TransactionType, decimal Amount, decimal TotalBalance, DateTime Timestamp, string AccountId);

public class AccountHierarchyTransactionsDto
{
    public required string StreamId { get; set; }

    public IList<TransactionEntry> Transactions { get; init; } = new List<TransactionEntry>();

}