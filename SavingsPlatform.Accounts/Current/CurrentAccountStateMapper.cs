using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Current.Models;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Common.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

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
                PlatformId = state.Data.PlatformId ?? string.Empty,
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
                    dto.TotalBalance,
                    dto.PlatformId),
                    HasUnpublishedEvents = dto.HasUnpublishedEvents,
                    UnpublishedEventsJson = dto.UnpublishedEvents?.Any() ?? false ?
                        JsonSerializer.Serialize(Enumerable.Cast<object>(dto.UnpublishedEvents)) : null
            };
        }
    }
}
