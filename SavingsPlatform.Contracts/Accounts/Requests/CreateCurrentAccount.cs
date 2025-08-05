using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Contracts.Accounts.Requests;

public record CreateCurrentAccount(string ExternalRef, Currency AccountCurrency, string? UserId = null);
