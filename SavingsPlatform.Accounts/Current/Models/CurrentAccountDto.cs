using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Accounts.Current.Models;

public record CurrentAccountDto(
    string Id,
    string ExternalRef,
    DateTime? OpenedOn,
    decimal TotalBalance,
    AccountType Type = AccountType.CurrentAccount);
