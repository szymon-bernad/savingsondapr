namespace SavingsPlatform.Accounts.AccountHolders;

public record AccountHolderDto(DateTime AddedOn, string? Username = default, ICollection<string> AccountIds = null);
