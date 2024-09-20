using MediatR;

namespace SavingsPlatform.Contracts.Accounts.Commands;

public record DebitAccountCommand(string MsgId, string ExternalRef, decimal Amount, DateTime TransactionDate, string? TransferRef) : IRequest;
