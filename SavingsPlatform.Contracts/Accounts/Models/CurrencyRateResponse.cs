using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Contracts.Accounts.Models;

public record CurrencyRateResponse(Currency AccountCurrency, decimal InterestRate, DateTime Timestamp);
