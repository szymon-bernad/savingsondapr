using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavingsPlatform.Contracts.Accounts.Events
{
    public record AccountCredited(
        string Id,
        string ExternalRef,
        string AccountId,
        decimal Amount,
        string? TransferId,
        DateTime Timestamp,
        string EventType,
        AccountType AccountType,
        string? PlatformId) : IEvent;

}
