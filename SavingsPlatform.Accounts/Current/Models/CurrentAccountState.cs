﻿using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Accounts.Current.Models;

public record CurrentAccountState : IAccountAggregateStateEntry
{
    public required string Key { get; init; } = string.Empty;
    public required string ExternalRef { get; init; } = string.Empty;
    public DateTime? OpenedOn { get; set; }
    public decimal TotalBalance { get; set; }
    public bool HasUnpublishedEvents { get; set; } = false;
    public ICollection<object>? UnpublishedEvents { get; set; } = default;

    public Currency Currency { get; set; } = Currency.EUR;
    public AccountType Type { get; set; } = AccountType.CurrentAccount;

    public string CurrentAccountId => this.Key;
}
