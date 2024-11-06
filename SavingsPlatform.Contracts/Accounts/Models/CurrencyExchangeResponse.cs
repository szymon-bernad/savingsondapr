namespace SavingsPlatform.Contracts.Accounts.Models;

public record CurrencyExchangeResponse(decimal TargetAmount, decimal Rate, string ExchangeType, DateTime Timestamp);
