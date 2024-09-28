using MediatR;
using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Contracts.Accounts.Commands;

public record DebitAccountCommand(string MsgId, string ExternalRef, decimal Amount, DateTime TransactionDate, AccountType Type, string? TransferRef) : ICommandRequest;
