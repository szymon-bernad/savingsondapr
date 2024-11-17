using System.Text.Json.Serialization;

namespace SavingsPlatform.Contracts.Accounts.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExchangeOrderType
{
    GuaranteedRate,
    MarketRate,
    LimitOrder,
}
