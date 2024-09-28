using SavingsPlatform.Accounts.Current.Models;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Common.Repositories;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SavingsPlatform.Accounts.Current
{
    internal class CurrentAccountStateMapper : IStateMapper<AggregateState<CurrentAccountDto>, CurrentAccountState>
    {
        public CurrentAccountState Map(AggregateState<CurrentAccountDto> state)
        {
            var events = !string.IsNullOrEmpty(state.UnpublishedEventsJson) ?
                JsonSerializer.Deserialize<IEnumerable<JsonNode>>(state!.UnpublishedEventsJson) : Enumerable.Empty<JsonNode>();

            var unpubEvents = events is not null ? Enumerable.Cast<object>(events.Select(e => e.AsObject())).ToList() : null;

            return new CurrentAccountState
            {
                Key = state.Data!.Id,
                ExternalRef = state.Data!.ExternalRef,
                OpenedOn = state.Data.OpenedOn,
                TotalBalance = state.Data!.TotalBalance,
                HasUnpublishedEvents = state.HasUnpublishedEvents,
                UnpublishedEvents = unpubEvents
            };
        }

        public AggregateState<CurrentAccountDto> ReverseMap(CurrentAccountState dto)
        {
            return new AggregateState<CurrentAccountDto>
            {
                Id = dto.Key,
                Data = new CurrentAccountDto(
                    dto.Key,
                    dto.ExternalRef,
                    dto.OpenedOn,
                    dto.TotalBalance),
                    HasUnpublishedEvents = dto.HasUnpublishedEvents,
                    UnpublishedEventsJson = dto.UnpublishedEvents?.Any() ?? false ?
                        JsonSerializer.Serialize(Enumerable.Cast<object>(dto.UnpublishedEvents)) : null
            };
        }
    }
}
