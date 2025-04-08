using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Contracts.Accounts.Requests;

public record CurrencyExchangeSummaryRequest
{
    public required Currency SourceCurrency { get; init; }
    public required Currency TargetCurrency { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }

}
