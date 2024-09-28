namespace SavingsPlatform.Contracts.Accounts.Requests
{
    public record CreditAccount(string ExternalRef, decimal Amount, DateTime TransactionDate, string? MsgId);
}
