namespace SavingsPlatform.Contracts.Accounts.Commands;

public record CreateInstantSavingsAccountCommand(
    string MsgId,
    string ExternalRef,
    decimal InterestRate,
    string CurrentAccountId) : ICommandRequest;