using Dapr.Client;
using Microsoft.Extensions.Options;
using SavingsPlatform.Accounts.Config;
using SavingsPlatform.Contracts.Accounts.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.JavaScript;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace SavingsPlatform.Common.Services
{
    public class DaprEventPublishingService : IEventPublishingService
    {
        private readonly string PubSubName;
        private readonly string CommandsTopicName;
        private readonly DaprClient _daprClient;

        public DaprEventPublishingService(
            DaprClient daprClient,
            IOptions<SavingsAccountsStateStoreConfig> stateStoreCfg)
        {
            PubSubName = stateStoreCfg?.Value?.PubSubName
                ?? throw new ArgumentNullException(nameof(PubSubName));
            CommandsTopicName = stateStoreCfg?.Value?.CommandsTopicName
                ?? throw new ArgumentNullException(nameof(CommandsTopicName));
            _daprClient = daprClient;
        }

        public Task PublishCommand<T>(T command)
        {
            var cmdString = JsonSerializer.Serialize(command);
            var cmdData = JsonNode.Parse(cmdString);
            if (cmdData is not null)
            {
                return _daprClient.PublishEventAsync(
                    PubSubName,
                    CommandsTopicName,
                    new PubSubCommand { CommandType = typeof(T).AssemblyQualifiedName!, Data = cmdData.AsObject() });
            }

            throw new InvalidOperationException("Failed to serialize command data");
        }

        public Task PublishEvents(ICollection<object> events)
        {
            return Task.WhenAll(
                events.Select(e =>
                {
                    var jsonObject = e as JsonObject;
                    if (jsonObject is not null)
                    {
                        var evtType = jsonObject["EventType"]?.GetValue<string>().ToLower();
                        if (!string.IsNullOrEmpty(evtType))
                        {
                            return _daprClient.PublishEventAsync(PubSubName, evtType, jsonObject);
                        }
                    }
                    else
                    {
                        var evtType = e.GetType().GetProperty("EventType")?.GetValue(e)?.ToString()?.ToLower();
                        if (!string.IsNullOrEmpty(evtType))
                        {
                            return _daprClient.PublishEventAsync(PubSubName, evtType, e);
                        }
                    }
                    return Task.CompletedTask;
                }));
        }
    }
}
