using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Contracts.Accounts.Requests;

public record CreateSavingsAccount(string ExternalRef, decimal InterestRate, string CurrentAccountRef, Currency AccountCurrency, string UserId);
