using SavingsPlatform.Contracts.Accounts;

namespace SavingsPlatform.Accounts.AccountHolders;

public record AccountHolderDto(DateTime AddedOn, string? Username = default, ICollection<AccountInfo>? Accounts = default);
