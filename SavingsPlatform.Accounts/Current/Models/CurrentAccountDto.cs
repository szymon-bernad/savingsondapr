using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Accounts.Current.Models;

public record CurrentAccountDto(
    DateTime? OpenedOn,
    decimal TotalBalance,
    Currency Currency = Currency.EUR,
    AccountType Type = AccountType.CurrentAccount);
