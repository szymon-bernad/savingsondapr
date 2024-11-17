namespace CurrencyExchange.Api.Internal.Activities;

public enum ConfirmationStatus
{
    Confirmed,
    Rejected,
    Deferred
}

public record OrderConfirmationResult(ConfirmationStatus Status, string? TransactionId = null, decimal? EffectiveRate = null, decimal? TargetAmount = null);