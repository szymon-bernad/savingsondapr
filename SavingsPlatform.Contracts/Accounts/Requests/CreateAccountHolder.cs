namespace SavingsPlatform.Contracts.Accounts.Requests;

public record CreateAccountHolder(string Id, string Username, string ExternalRef, string[] AccountIds);
