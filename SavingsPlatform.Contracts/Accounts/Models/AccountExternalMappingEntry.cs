using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Contracts.Accounts.Models
{
    public record AccountExternalMappingEntry(string ExternalRef, string AccountId, AccountType Type);
}
