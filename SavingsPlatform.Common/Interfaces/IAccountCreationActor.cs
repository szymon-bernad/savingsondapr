using Dapr.Actors;
using SavingsPlatform.Contracts.Accounts.Models;
using SavingsPlatform.Contracts.Accounts.Requests;

namespace SavingsPlatform.Common.Interfaces;

public interface IAccountCreationActor : IActor
{
    Task InitiateAsync(AccountCreationData data);

    Task HandleAccountCreatedAsync(string accountId);
}
