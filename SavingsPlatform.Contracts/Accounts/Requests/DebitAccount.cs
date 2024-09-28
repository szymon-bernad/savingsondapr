using MediatR;

namespace SavingsPlatform.Contracts.Accounts.Requests
{
    public record DebitAccount(string ExternalRef, decimal Amount, DateTime TransactionDate, string? MsgId);
}
