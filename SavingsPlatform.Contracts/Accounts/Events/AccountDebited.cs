using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SavingsPlatform.Contracts.Accounts.Events
{
    public record AccountDebited(
    string Id,
    string ExternalRef,
    string AccountId,
    decimal Amount,
    string? TransferId,
    DateTime Timestamp,
    decimal TotalBalance,
    string EvtType,
    AccountType AccountType,
    string CurrentAccountId) : IEvent;
}
