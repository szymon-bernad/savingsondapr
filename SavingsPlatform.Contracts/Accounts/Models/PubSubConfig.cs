namespace SavingsPlatform.Contracts.Accounts.Models
{
    public class PubSubConfig
    {
        public string PubSubName { get; set; } = "pubsub";
        public string TopicName { get; set; } = "savingsaccountsevents";
        public string CommandsTopicName { get; set; } = "commands";
    }
}
