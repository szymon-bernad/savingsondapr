namespace SavingsPlatform.Contracts.Accounts.Interfaces
{
    public interface IEvent
    {
        string Id { get; }
        DateTime Timestamp { get; }
        string EvtType { get; }
        string CurrentAccountId { get; }
    }
}
