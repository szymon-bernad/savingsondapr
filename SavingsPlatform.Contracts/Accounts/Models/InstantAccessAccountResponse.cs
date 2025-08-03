using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Contracts.Accounts.Models;

public record InstantAccessAccountResponse(
    string Key, 
    string ExternalRef,
    DateTime? OpenedOn,
    DateTime? ActivatedOn,
    decimal InterestRate,
    decimal TotalBalance,
    decimal AccruedInterest,
    string CurrentAccountId,
    Currency Currency,
    AccountType Type)
{
    public BaseAccountResponse ToBase()
    {
        var details = new Dictionary<string, string>
        {
            { $"{nameof(InterestRate)}", (InterestRate*100m).ToString("F2") },
            { $"{nameof(AccruedInterest)}", AccruedInterest.ToString("F2") },
            { $"{nameof(CurrentAccountId)}", CurrentAccountId },
            { $"{nameof(ActivatedOn)}", ActivatedOn?.ToString("yyyy-MM-dd HH:mm:ss.fff") ?? string.Empty },
        };

        return new BaseAccountResponse(
            Key,
            ExternalRef,
            OpenedOn,
            TotalBalance,
            Currency,
            Type,
            details);
    }
}
