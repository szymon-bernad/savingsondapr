using MediatR;
using SavingsPlatform.Contracts.Accounts.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavingsPlatform.Contracts.Accounts.Commands
{
    public record PublishEventsCommand(
        string AccountId,
        AccountType AccountType
        ) : IRequest;
}
