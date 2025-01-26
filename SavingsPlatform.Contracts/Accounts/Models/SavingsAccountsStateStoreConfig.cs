using SavingsPlatform.Contracts.Accounts.Models;

namespace SavingsPlatform.Accounts.Config
{
    public class SavingsAccountsStateStoreConfig : PubSubConfig
    {
        public string StateStoreName { get; set; } = "statestore";
    }
}
