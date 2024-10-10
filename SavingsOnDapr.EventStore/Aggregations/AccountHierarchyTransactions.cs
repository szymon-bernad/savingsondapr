using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Events;
using SavingsPlatform.Contracts.Accounts.Models;

namespace SavingsOnDapr.EventStore.Aggregations
{
    public class AccountHierarchyTransactions(DateTime? fromDate, DateTime? toDate)
    {
        private readonly DateTime? _fromDate = fromDate;
        private readonly DateTime? _toDate = toDate;

        public IList<TransactionEntry> TransactionEntries { get; init; } = new List<TransactionEntry>();

        public string? Id { get; set; }

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

            TransactionEntries.Add(new TransactionEntry(evt.Id, TransactionType.Credit, evt.Amount, evt.TotalBalance, evt.Timestamp, evt.AccountId));
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

            TransactionEntries.Add(new TransactionEntry(evt.Id, TransactionType.Credit, evt.Amount, evt.TotalBalance, evt.Timestamp, evt.AccountId));
        }

        public AccountHierarchyTransactionsDto MapToDto()
        {
            return new AccountHierarchyTransactionsDto
            {
                StreamId = Id ?? string.Empty,
                Transactions = TransactionEntries
            };
        }
    }
}
