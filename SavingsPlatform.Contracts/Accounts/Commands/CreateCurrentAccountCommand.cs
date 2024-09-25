namespace SavingsPlatform.Contracts.Accounts.Commands;

public record CreateCurrentAccountCommand(
    string MsgId,
    string ExternalRef,
    string PlatformId) : ICommandRequest;
