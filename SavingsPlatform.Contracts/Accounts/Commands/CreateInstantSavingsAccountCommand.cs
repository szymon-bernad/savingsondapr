using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Contracts.Accounts.Commands;

public record CreateInstantSavingsAccountCommand(
    string MsgId,
    string ExternalRef,
    decimal InterestRate,
    string CurrentAccountId,
    string UserId,
    Currency AccountCurrency = Currency.EUR) : ICommandRequest;