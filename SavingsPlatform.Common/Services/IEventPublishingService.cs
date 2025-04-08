using SavingsPlatform.Contracts.Accounts.Commands;

namespace SavingsPlatform.Common.Services;

public interface IEventPublishingService
{
    Task PublishEvents(ICollection<object> events);

    Task PublishEventsToTopic(string topic, ICollection<object> events);

    Task PublishCommand<T>(T command) where T : ICommandRequest;
}
