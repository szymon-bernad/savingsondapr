using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Models;

namespace CurrencyExchange.Api.Internal;

public interface IExchangeRatesService
{
    Task<ExchangeRatesData> GetCurrentRatesAsync(Currency source, Currency target);
}
