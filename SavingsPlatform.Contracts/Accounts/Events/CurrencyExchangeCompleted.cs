using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Interfaces;

namespace SavingsPlatform.Contracts.Accounts.Events;

public record CurrencyExchangeCompleted(
    string Id,
    string DebtorExternalRef,
    string BeneficiaryExternalRef,
    string CurrentAccountId,
    string TransactionId, 
    decimal EffectiveRate,
    decimal SourceAmount,
    Currency SourceCurrency,
    Currency TargetCurrency,
    string OrderId,
    ExchangeOrderType OrderType,
    DateTime Timestamp,
    string EvtType) : IEvent;
