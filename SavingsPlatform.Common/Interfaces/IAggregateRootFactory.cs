using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavingsPlatform.Common.Interfaces
{
    public interface IAggregateRootFactory<T, TState>
        where T : IAggregateRoot<TState>
        where TState : IAggregateStateEntry
    {
        Task<T> GetInstanceAsync(string? id = default);

        Task<T> GetInstanceByExternalRefAsync(string externalRef);

        Task<T?> TryGetInstanceAsync(string id);

        Task<T?> TryGetInstanceByExternalRefAsync(string externalRef);
    }
}
