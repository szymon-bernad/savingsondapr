using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Models;

namespace CurrencyExchange.Api.Internal;

public interface IExchangeRatesService
{
    Task<ExchangeRatesData> SetExchangeBaseRateAsync(Currency source, Currency target, decimal rate);

    Task<ExchangeRatesData> GetCurrentRatesAsync(Currency source, Currency target, bool refreshCache = false);
}
