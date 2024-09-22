using MediatR;

namespace SavingsPlatform.Contracts.Accounts.Commands;

public record CreditAccountCommand(string MsgId, string ExternalRef, decimal Amount, DateTime TransactionDate, string? TransferRef) : ICommandRequest;
