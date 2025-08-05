using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Contracts.Accounts.Commands;


public record CreateAccountRequest(
    string MsgId,
    string ExternalRef,
    string UserId,
    AccountType Type = AccountType.CurrentAccount,
    Currency AccountCurrency = Currency.EUR,
    SavingsAccountDetails? SavingsDetails = default) : ICommandRequest;

public record SavingsAccountDetails(
    decimal InterestRate,
    string CurrentAccountRef);
