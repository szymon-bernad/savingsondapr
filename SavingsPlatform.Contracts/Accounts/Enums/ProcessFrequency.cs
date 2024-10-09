using System.Text.Json.Serialization;

namespace SavingsPlatform.Contracts.Accounts.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ProcessFrequency
    {
        Daily,
        Weekly,
        Monthly,
        Quarterly,
        Yearly,
    }
}
