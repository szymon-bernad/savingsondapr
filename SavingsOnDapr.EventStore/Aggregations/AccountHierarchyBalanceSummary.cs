using Google.Protobuf.WellKnownTypes;
using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Events;
using SavingsPlatform.Contracts.Accounts.Models;

namespace SavingsOnDapr.EventStore.Aggregations
{
    public class AccountHierarchyBalanceSummary(DateTime? fromDate, DateTime? toDate)
    {
        public IDictionary<string, AccountBalanceRangeEntry> BalanceRanges { get; init; } 
            = new Dictionary<string, AccountBalanceRangeEntry>();

        public string? Id { get; set; }

        public void Apply(AccountCredited evt)
        {
            if (!IsEventWithinDateRange(evt.Timestamp))
            {
                return;
            }

            var prevBalance = evt.TotalBalance - evt.Amount;
            ApplyBalanceChange(evt.AccountId, prevBalance, evt.TotalBalance);
        }

        public void Apply(AccountDebited evt)
        {
            if (!IsEventWithinDateRange(evt.Timestamp))
            {
                return;
            }

            var prevBalance = evt.TotalBalance + evt.Amount;
            ApplyBalanceChange(evt.AccountId, prevBalance, evt.TotalBalance);
        }

        public void Apply(AccountInterestApplied evt)
        {
            if (!IsEventWithinDateRange(evt.Timestamp))
            {
                return;
            }

            var prevBalance = evt.TotalBalance - evt.Amount;
            ApplyBalanceChange(evt.AccountId, prevBalance, evt.TotalBalance);
        }

        public void ApplyBalanceChange(string accountId, decimal prevBalance, decimal totalBalance)
        {
            var lowerBalance = Math.Min(prevBalance, totalBalance);
            var upperBalance = Math.Max(prevBalance, totalBalance);

            if (!BalanceRanges.ContainsKey(accountId))
            {
                var balanceRange = new AccountBalanceRangeEntry(accountId, lowerBalance, upperBalance);
                BalanceRanges.Add(accountId, balanceRange);
            }
            else
            {
                var balanceRange = BalanceRanges[accountId] with
                {
                    MinTotalBalance = Math.Min(BalanceRanges[accountId].MinTotalBalance, lowerBalance),
                    MaxTotalBalance = Math.Max(BalanceRanges[accountId].MaxTotalBalance, upperBalance),
                };

                BalanceRanges[accountId] = balanceRange;
            }
        }

        public AccountHierarchyBalanceRangesDto MapToDto()
        {
            return new AccountHierarchyBalanceRangesDto
            {
                StreamId = Id ?? string.Empty,
                BalanceRanges = BalanceRanges,
            };
        }

        private bool IsEventWithinDateRange(DateTime timestamp)
        {
            return (!fromDate.HasValue || timestamp >= fromDate.Value) &&
                   (!toDate.HasValue || timestamp <= toDate.Value);
        }
    }
}
