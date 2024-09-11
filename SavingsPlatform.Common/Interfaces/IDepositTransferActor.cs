using Dapr.Actors;
using SavingsPlatform.Contracts.Accounts.Models;

namespace SavingsPlatform.Common.Interfaces
{
    public interface IDepositTransferActor : IActor
    {
        public Task InitiateTransferAsync(DepositTransferData data);

        public Task HandleDebitedEventAsync();

        public Task HandleCreditedEventAsync();

        public Task HandleStartAfterAccountCreation(string savingsAccountId, string settlementAccountId);

    }
}
