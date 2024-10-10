namespace SavingsPlatform.Contracts.Accounts.Commands;

public record AccrueInterestForAccountsCommand(
    string MsgId,
    string CurrentAccountId,
    string[] SavingsAccountIds,
    DateTime AccrualDate
    ) : ICommandRequest;
