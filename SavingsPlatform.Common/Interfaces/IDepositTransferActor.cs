using Dapr.Actors;
using SavingsPlatform.Contracts.Accounts.Models;

namespace SavingsPlatform.Common.Interfaces
{
    public interface IDepositTransferActor : IActor
    {
        public Task InitiateTransferAsync(DepositTransferData data);

        public Task HandleDebitedEventAsync(string accountId);

        public Task HandleCreditedEventAsync(string accountId);

    }
}
