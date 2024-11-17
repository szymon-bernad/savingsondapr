using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Interfaces;

namespace SavingsPlatform.Contracts.Accounts.Events
{
    public record AccountCreated(
        string Id,
        string ExternalRef,
        string CurrentAccountId, 
        string AccountId, 
        AccountType AccountType,
        Currency Currency,
        DateTime Timestamp, 
        string EvtType) : IEvent;

}
