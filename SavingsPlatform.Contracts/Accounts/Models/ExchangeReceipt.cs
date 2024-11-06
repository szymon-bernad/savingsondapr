using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Contracts.Accounts.Models;

public record ExchangeResult(bool Succeeded, string? Message, ExchangeReceipt? Receipt);

public record ExchangeReceipt(
    decimal TargetAmount,
    decimal ExchangeRate,
    DateTime TransactionDate,
    string TransactionId,
    string DebtorExternalRef,
    string BeneficiaryExternalRef,
    Currency SourceCurrency,
    Currency TargetCurrency);
