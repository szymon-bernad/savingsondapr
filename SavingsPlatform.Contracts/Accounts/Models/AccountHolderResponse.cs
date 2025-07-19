namespace SavingsPlatform.Contracts.Accounts.Models;

public record AccountHolderResponse(string Id, string Username, ICollection<AccountInfo> Accounts);
