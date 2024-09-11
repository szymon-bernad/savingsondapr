using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavingsPlatform.Accounts.Aggregates.InstantAccess.Models
{
    public record InstantAccessSavingsAccountDto(
        string Id,
        string ExternalRef,
        DateTime? OpenedOn,
        DateTime? ActivatedOn,
        decimal InterestRate,
        decimal TotalBalance,
        decimal AccruedInterest,
        Guid? LastTransactionId,
        string? PlatformId,
        ProcessFrequency InterestApplicationFrequency = ProcessFrequency.Weekly,
        DateTime? InterestApplicationDueOn = null,
        AccountType Type = AccountType.SavingsAccount);
}
