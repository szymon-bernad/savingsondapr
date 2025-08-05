using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Contracts.Accounts.Models;

public interface IBaseAccountResponse
{
    string Key { get; }
    string ExternalRef { get; }
    DateTime? OpenedOn { get; }
    decimal TotalBalance { get; }
    Currency Currency { get; }
    AccountType Type { get; }
    IDictionary<string, string>? Details { get; }
}
