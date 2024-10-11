namespace SavingsPlatform.Contracts.Accounts.Commands;

public record AccrueInterestCommand(
    string MsgId,
    string AccountId,
    string ExternalRef,
    DateTime AccrualDate,
    DateTime AccrualFrom,
    decimal? AdjustedBalance = null
    ) : ICommandRequest;