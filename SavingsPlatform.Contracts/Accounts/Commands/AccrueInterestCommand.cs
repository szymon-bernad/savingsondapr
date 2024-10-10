namespace SavingsPlatform.Contracts.Accounts.Commands;

public record AccrueInterestCommand(
    string MsgId,
    string AccountId,
    DateTime AccrualDate,
    decimal? AdjustedBalance = null
    ) : ICommandRequest;