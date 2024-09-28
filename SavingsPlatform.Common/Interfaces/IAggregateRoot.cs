namespace SavingsPlatform.Common.Interfaces;

public interface IAggregateRoot<T> where T : IAggregateStateEntry
{
    public T? State { get; }

}
