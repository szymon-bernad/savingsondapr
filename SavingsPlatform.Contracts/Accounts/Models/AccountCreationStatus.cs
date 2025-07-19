using System.Text.Json.Serialization;

namespace SavingsPlatform.Contracts.Accounts.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AccountCreationStatus
{
    New,
    CreatedUnassigned,
    FailedDuplicateExternalRef,
    FailedInvalidUserId,
    Completed,
}
