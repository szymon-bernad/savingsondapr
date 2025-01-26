using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsOnDapr.EventStore.Aggregations;

public class CurrencyExchangeSummaryDto
{
    public DateOnly SummaryDate { get; init; }
    public int TotalCountOfExchanges { get; set; }
    public decimal TotalSourceAmountOfExchanges { get; set; }
    public decimal TotalTargetAmountOfExchanges { get; set; }
    public Currency SourceCurrency { get; init; }
    public Currency TargetCurrency { get; init; }
    public ICollection<CurrencyExchangeDto> Entries { get; init; } = new List<CurrencyExchangeDto>();
}


public record CurrencyExchangeDto(
    DateTime ExchangeDate,
    decimal SourceAmount,
    decimal TargetAmount,
    Currency SourceCurrency,
    Currency TargetCurrency,
    ExchangeOrderType OrderType,
    string DebtorExternalRef,
    string BeneficiaryExternalRef);
