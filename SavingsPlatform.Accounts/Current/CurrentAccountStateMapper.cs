using SavingsPlatform.Accounts.Current.Models;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Common.Repositories;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SavingsPlatform.Accounts.Current;

internal class CurrentAccountStateMapper : IStateMapper<AggregateState<CurrentAccountDto>, CurrentAccountState>
{
    public CurrentAccountState Map(AggregateState<CurrentAccountDto> stateDto)
    {
        var events = !string.IsNullOrEmpty(stateDto.UnpublishedEventsJson) ?
            JsonSerializer.Deserialize<IEnumerable<JsonNode>>(stateDto!.UnpublishedEventsJson) : Enumerable.Empty<JsonNode>();

        var unpubEvents = events is not null ? Enumerable.Cast<object>(events.Select(e => e.AsObject())).ToList() : null;
        return new CurrentAccountState
        {
            Key = stateDto.Id,
            ExternalRef = stateDto.ExternalRef,
            OpenedOn = stateDto.Data!.OpenedOn,
            TotalBalance = stateDto.Data!.TotalBalance,
            HasUnpublishedEvents = stateDto.HasUnpublishedEvents,
            UnpublishedEvents = unpubEvents
        };
    }

    public AggregateState<CurrentAccountDto> ReverseMap(CurrentAccountState dto)
    {
        return new AggregateState<CurrentAccountDto>
        {
            Id = dto.Key,
            ExternalRef = dto.ExternalRef,
            Data = new CurrentAccountDto(
                dto.OpenedOn,
                dto.TotalBalance),
            HasUnpublishedEvents = dto.HasUnpublishedEvents,
            UnpublishedEventsJson = dto.UnpublishedEvents?.Any() ?? false ?
            JsonSerializer.Serialize(Enumerable.Cast<object>(dto.UnpublishedEvents)) : null
        };
    }
}