using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Contracts.Accounts.Models;

public record BaseAccountResponse(
    string Key,
    string ExternalRef,
    DateTime? OpenedOn,
    decimal TotalBalance,
    Currency Currency,
    AccountType Type,
    IDictionary<string, string>? Details = default) : IBaseAccountResponse;
