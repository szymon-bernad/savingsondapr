using Marten.Schema;

namespace SavingsPlatform.Common.Repositories;

public class AggregateState<T> : IAggregateState
{
    public string Id { get; set; } = string.Empty;
    public string ExternalRef { get; set; } = string.Empty;
    public T? Data { get; set; }
    public bool HasUnpublishedEvents { get; set; } = false;
    public string? UnpublishedEventsJson { get; set; } = default;

    [Version]
    public Guid Version { get; set; } = Guid.Empty;
}
