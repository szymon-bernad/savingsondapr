using Dapr.Actors;
using SavingsPlatform.Contracts.Accounts.Events;
using SavingsPlatform.Contracts.Accounts.Models;

namespace SavingsPlatform.Common.Interfaces
{
    public interface IInterestAccrualActor : IActor
    {
        Task InitiateAsync(InterestAccrualData data);

        Task HandleAccruedEventAsync(DateTime timestamp);
    }
}
