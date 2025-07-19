using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Contracts.Accounts.Commands;

public record CreateCurrentAccountCommand(
    string MsgId,
    string ExternalRef,
    string UserId,
    Currency AccountCurrency = Currency.EUR) : ICommandRequest;
