using MediatR;

namespace SavingsPlatform.Contracts.Accounts.Commands;

public interface ICommandRequest : IRequest
{
    string MsgId { get; }
}
