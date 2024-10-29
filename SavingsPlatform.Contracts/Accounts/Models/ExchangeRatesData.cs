using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Contracts.Accounts.Models;

public record ExchangeRatesData
{
    public required Currency SourceCurrency { get; init; }

    public required Currency TargetCurrency { get; init; } 

    public ICollection<ExchangeRateEntry> ExchangeRates { get; init; } = new List<ExchangeRateEntry>();
}

public record ExchangeRateEntry(decimal LowerBound, decimal UpperBound, decimal Rate)
{
    public bool IsInRange(decimal value) => value >= LowerBound && value < UpperBound;
}
