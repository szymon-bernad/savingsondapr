using Marten.Schema;

namespace SavingsPlatform.Contracts.Accounts.Models;

public record EventStatusEntry
{
    [Identity]
    public required string EventId { get; init; }

    public DateTime ProcessedOn { get; init; } = DateTime.UtcNow; 
}
