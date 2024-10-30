namespace CurrencyExchange.Api.Internal;

public record OrderConfirmationResult(string? TransactionId, bool IsConfirmed, decimal? EffectiveRate, decimal? TargetAmount);