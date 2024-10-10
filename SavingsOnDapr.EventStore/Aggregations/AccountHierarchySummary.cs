using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Events;
using System.Security.Cryptography.Xml;

namespace SavingsOnDapr.EventStore.Aggregations;

public class AccountHierarchySummary(DateTime? fromDate, DateTime? toDate)
{
    private readonly DateTime? _fromDate = fromDate;
    private readonly DateTime? _toDate = toDate;

    private HashSet<string> _transfers = new HashSet<string>();

    public string? Id { get; set; }

    public decimal TotalAmountTransferredToSavings { get; private set; } = default;

    public decimal TotalAmountWithdrawnFromSavings { get; private set; } = default;

    public decimal TotalAmountOfNewDeposits { get; private set; } = default;

    public decimal TotalAmountOfWithdrawals { get; private set; } = default;

    public int TotalCountOfDepositTransfers { get; private set; } = 0;

    public void Apply(AccountCredited evt)
    {
        if (_fromDate.HasValue && evt.Timestamp < _fromDate.Value)
        {
            return;
        }

        if (_toDate.HasValue && evt.Timestamp > _toDate.Value)
        {
            return;
        }

        if (!string.IsNullOrEmpty(evt.TransferId))
        {
            if (!_transfers.Contains(evt.TransferId))
            {
                ++TotalCountOfDepositTransfers;
                _transfers.Add(evt.TransferId);
            }

            if (evt.AccountType == AccountType.SavingsAccount)
            {
                TotalAmountTransferredToSavings += evt.Amount;
            }
            else if (evt.AccountType == AccountType.CurrentAccount)
            {
                TotalAmountWithdrawnFromSavings += evt.Amount;
            }
        }
        else if (evt.AccountType == AccountType.CurrentAccount)
        {
            TotalAmountOfNewDeposits += evt.Amount;
        }
    }

    public void Apply(AccountDebited evt)
    {
        if (_fromDate.HasValue && evt.Timestamp < _fromDate.Value)
        {
            return;
        }

        if (_toDate.HasValue && evt.Timestamp > _toDate.Value)
        {
            return;
        }

        if (!string.IsNullOrEmpty(evt.TransferId))
        {
            if (!_transfers.Contains(evt.TransferId))
            {
                ++TotalCountOfDepositTransfers;
                _transfers.Add(evt.TransferId);
            }
        }
        else if (evt.AccountType == AccountType.CurrentAccount)
        {
            TotalAmountOfWithdrawals += evt.Amount;
        }
    }
}
