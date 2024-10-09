using System.Text.Json.Serialization;

namespace SavingsPlatform.Common.Repositories.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum QueryOperator
{
    Equal,
    NotEqual,
    LessThan,
    GreaterThan,
    LessThanOrEqual,
    GreaterThanOrEqual,
}
