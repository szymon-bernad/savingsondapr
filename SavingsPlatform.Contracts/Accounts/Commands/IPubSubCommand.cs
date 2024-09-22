using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace SavingsPlatform.Contracts.Accounts.Commands
{
    public interface IPubSubCommand
    {
        string CommandType { get;}

        object Data { get; }

        string MsgId { get; }
    }
}
