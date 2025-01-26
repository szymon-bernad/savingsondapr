using SavingsPlatform.Contracts.Accounts.Events;

namespace SavingsOnDapr.EventStore.Aggregations;

public class CurrencyExchangeSummary
{
    private CurrencyExchangeSummaryDto? _currentExchange;

    public string? Id { get; set; }

    public void Apply(CurrencyExchangeCompleted evt)
    {
        var targetAmount = Math.Round(evt.EffectiveRate * evt.SourceAmount, 2, MidpointRounding.ToEven);
        var cexchDto = new CurrencyExchangeDto(
                        evt.Timestamp,
                        evt.SourceAmount,
                        targetAmount,
                        evt.SourceCurrency,
                        evt.TargetCurrency,
                        evt.OrderType,
                        evt.DebtorExternalRef,
                        evt.BeneficiaryExternalRef);

        if (_currentExchange is null)
        {
            _currentExchange = new CurrencyExchangeSummaryDto
            {
                SummaryDate = new DateOnly(evt.Timestamp.Date.Year, evt.Timestamp.Date.Month, evt.Timestamp.Date.Day),
                SourceCurrency = evt.SourceCurrency,
                TargetCurrency = evt.TargetCurrency,
                TotalCountOfExchanges = 1,
                TotalSourceAmountOfExchanges = evt.SourceAmount,
                TotalTargetAmountOfExchanges = targetAmount,
                Entries = new List<CurrencyExchangeDto> { cexchDto }
            };
        }
        else
        {
            _currentExchange.TotalCountOfExchanges++;
            _currentExchange.TotalSourceAmountOfExchanges += evt.SourceAmount;
            _currentExchange.TotalTargetAmountOfExchanges += targetAmount;
            _currentExchange.Entries.Add(cexchDto);
        }
    }

    public CurrencyExchangeSummaryDto MapToDto()
    {
        if (_currentExchange is null)
        {
            return new CurrencyExchangeSummaryDto();
        }

        return _currentExchange;
    }
}
