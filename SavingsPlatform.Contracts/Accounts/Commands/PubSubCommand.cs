using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace SavingsPlatform.Contracts.Accounts.Commands;

public class PubSubCommand : IPubSubCommand
{
    public required string CommandType { get; set; }

    public required object Data { get; set; }

    public required string MsgId { get; set; }
}
