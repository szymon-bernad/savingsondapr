using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Contracts.Accounts.Commands;

public record CreateCurrentAccountCommand(
    string MsgId,
    string ExternalRef,
    Currency AccountCurrency = Currency.EUR) : ICommandRequest;
