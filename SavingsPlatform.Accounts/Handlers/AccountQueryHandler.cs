using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Current.Models;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts;
using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Models;

namespace SavingsPlatform.Accounts.Handlers;

public class AccountsQueryHandler(
    IStateEntryQueryHandler<CurrentAccountState> _repoCA,
    IStateEntryQueryHandler<InstantAccessSavingsAccountState> _repoIASA) : IAccountsQueryHandler
{
    public async Task<ICollection<AccountInfo>> FetchAccountInfosByIds(string[] accountIds)
    {
        var resultCA = await _repoCA.GetAccountsAsync(accountIds);
        var resultIASA = await _repoIASA.GetAccountsAsync([.. accountIds.Except(resultCA.Select(r => r.Key))]);

        var results = new List<AccountInfo>(
            resultCA.Select(
                x => new AccountInfo(x.Key, x.Type)));

        results.AddRange(
            resultIASA.Select(
                x => new AccountInfo(
                    x.Key,
                    x.Type)));

        return results;
    }

    public async Task<ICollection<BaseAccountResponse>> FetchAccountsByIds(string[] accountIds)
    {
        var resultCA = await _repoCA.GetAccountsAsync(accountIds);
        var resultIASA = await _repoIASA.GetAccountsAsync([.. accountIds.Except(resultCA.Select(r => r.Key))]);

        var results = new List<BaseAccountResponse>(
            resultCA.Select(
                x => new BaseAccountResponse(
                    x.Key,
                    x.ExternalRef,
                    x.OpenedOn,
                    x.TotalBalance,
                    x.Currency,
                    x.Type)));

        results.AddRange(
            resultIASA.Select(
                x => new InstantAccessAccountResponse(
                    x.Key, 
                    x.ExternalRef,
                    x.OpenedOn,
                    x.ActivatedOn,
                    x.InterestRate,
                    x.TotalBalance,
                    x.AccruedInterest,
                    x.CurrentAccountId,
                    x.Currency,
                    x.Type)
                .ToBase()));

        return results;
    }
}
