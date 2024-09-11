using Marten.Schema;

namespace SavingsPlatform.Contracts.Accounts;

public class MessageProcessedEntry
{
    [Identity] 
    public required string MessageId { get; set; }

    public required DateTime ProcessedOn { get; set; }
}
