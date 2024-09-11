namespace SavingsPlatform.Accounts.Config
{
    public class SavingsAccountsStateStoreConfig
    {
        public string StateStoreName { get; set; } = "statestore";
        public string PubSubName { get; set; } = "pubsub";
        public string TopicName { get; set; } = "savingsaccountsevents";
        public string CommandsTopicName { get; set; } = "commands";
    }
}
