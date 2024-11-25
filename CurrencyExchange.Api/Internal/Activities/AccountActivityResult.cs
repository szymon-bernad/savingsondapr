using System.Net;

namespace CurrencyExchange.Api.Internal.Activities;

public record AccountActivityResult(bool Succeeded, string? Reason = null, bool IsRetriable = true);
