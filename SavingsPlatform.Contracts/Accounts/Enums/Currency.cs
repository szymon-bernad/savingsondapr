using System.Text.Json.Serialization;

namespace SavingsPlatform.Contracts.Accounts.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Currency
{
    EUR,
    USD,
    GBP,
    CHF,
    PLN,
    NOK,
    CAD,
}
