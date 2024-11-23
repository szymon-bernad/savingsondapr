using Microsoft.Extensions.Caching.Memory;
using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Models;

namespace CurrencyExchange.Api.Internal;

internal class ExchangeRatesService : IExchangeRatesService
{
    private readonly IMemoryCache _cache;
    private readonly Random _random = new Random();

    private IDictionary<string, decimal> BaseRatesDict = new Dictionary<string, decimal>
    {
        { $"{Currency.USD}-{Currency.EUR}", 0.9m },
        { $"{Currency.EUR}-{Currency.USD}", 1.1m },
        { $"{Currency.USD}-{Currency.GBP}", 0.75m },
        { $"{Currency.GBP}-{Currency.USD}", 1.30m },
        { $"{Currency.EUR}-{Currency.GBP}", 0.85m },
        { $"{Currency.GBP}-{Currency.EUR}", 1.20m },
        { $"{Currency.PLN}-{Currency.EUR}", 0.22m },
        { $"{Currency.EUR}-{Currency.PLN}", 4.50m },
        { $"{Currency.NOK}-{Currency.EUR}", 0.10m },
        { $"{Currency.EUR}-{Currency.NOK}", 10.0m },
        { $"{Currency.CAD}-{Currency.EUR}", 0.65m },
        { $"{Currency.EUR}-{Currency.CAD}", 1.55m },
        { $"{Currency.CHF}-{Currency.EUR}", 0.95m },
        { $"{Currency.EUR}-{Currency.CHF}", 1.05m },
    };

    public ExchangeRatesService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<ExchangeRatesData> GetCurrentRatesAsync(Currency source, Currency target, bool refreshCache = false)
    {
        if (!refreshCache && _cache.TryGetValue($"{source}-{target}", out ExchangeRateEntry[]? rates))
        {
            return Task.FromResult(
                new ExchangeRatesData 
                { 
                    SourceCurrency = source,
                    TargetCurrency = target,
                    ExchangeRates = rates?.OrderBy(r => r.LowerBound)?.ToList() ?? new List<ExchangeRateEntry>(),
                });
        }
        else
        {
            var baseRate = BaseRatesDict[$"{source}-{target}"];
            var variableRate = (decimal)((_random.NextDouble() - 0.5) * 0.05);
            var exchangeRates = new[]
            {
                new ExchangeRateEntry(0, 1000, Math.Round((0.985m+variableRate)*baseRate,4,MidpointRounding.ToPositiveInfinity)),
                new ExchangeRateEntry(1000, 5000, Math.Round((1.005m+variableRate)*baseRate,4,MidpointRounding.ToPositiveInfinity)),
                new ExchangeRateEntry(5000, 50_000, Math.Round((1.025m+variableRate)*baseRate,4,MidpointRounding.ToPositiveInfinity)),
                new ExchangeRateEntry(50_000, 500_000, Math.Round((1.044m+variableRate)*baseRate,4,MidpointRounding.ToPositiveInfinity)),
                new ExchangeRateEntry(500_000, 1_999_999, Math.Round((1.055m+variableRate)*baseRate,4,MidpointRounding.ToPositiveInfinity)),
            };

            _cache.Set($"{source}-{target}", exchangeRates, TimeSpan.FromMinutes(10));

            return Task.FromResult(
                new ExchangeRatesData
                {
                    SourceCurrency = source,
                    TargetCurrency = target,
                    ExchangeRates = exchangeRates.OrderBy(r => r.LowerBound).ToList(),
                });
        }
    }

    public Task<ExchangeRatesData> SetExchangeBaseRateAsync(Currency source, Currency target, decimal rate)
    {
        BaseRatesDict[$"{source}-{target}"] = rate;

        return GetCurrentRatesAsync(source, target, true);
    }
}
