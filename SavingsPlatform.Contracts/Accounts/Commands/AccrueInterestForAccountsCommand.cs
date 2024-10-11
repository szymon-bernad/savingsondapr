namespace SavingsPlatform.Contracts.Accounts.Commands;

public record AccountAccrualEntry(string AccountId, string ExternalRef, DateTime? AccrualFrom);

public record AccrueInterestForAccountsCommand(
    string MsgId,
    string CurrentAccountId,
    IEnumerable<AccountAccrualEntry> SavingsAccountIds,
    DateTime AccrualDate) 
        : ICommandRequest;
