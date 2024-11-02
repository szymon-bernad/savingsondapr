using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Contracts.Accounts.Requests;

public record CurrencyExchangeOrder(
    string OrderId,
    string DebtorExternalRef,
    string BeneficiaryExternalRef,
    Currency SourceCurrency,
    Currency TargetCurrency,
    decimal SourceAmount,
    decimal? ExchangeRate,
    ExchangeOrderType OrderType = ExchangeOrderType.MarketRate);

