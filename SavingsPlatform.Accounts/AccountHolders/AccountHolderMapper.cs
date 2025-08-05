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

namespace SavingsPlatform.Accounts.AccountHolders;

internal class AccountHolderMapper : IStateMapper<AggregateState<AccountHolderDto>, AccountHolderState>
{
    public AccountHolderState Map(AggregateState<AccountHolderDto> state)
    {
        var events = !string.IsNullOrEmpty(state.UnpublishedEventsJson) ?
                JsonSerializer.Deserialize<IEnumerable<JsonNode>>(state!.UnpublishedEventsJson) :
                Enumerable.Empty<JsonNode>();

        var unpubEvents = events is not null ? 
            Enumerable.Cast<object>(events.Select(e => e.AsObject())).ToList() :
            null;

        return new AccountHolderState
        {
            Key = state.Id,
            ExternalRef = state.ExternalRef,
            Username = state.Data!.Username,
            Accounts = [.. state.Data!.Accounts],
            HasUnpublishedEvents = state.HasUnpublishedEvents,
            UnpublishedEvents = unpubEvents
        };
    }

    public AggregateState<AccountHolderDto> ReverseMap(AccountHolderState dto)
    {
        return new AggregateState<AccountHolderDto>
        {
            Id = dto.Key,
            ExternalRef = dto.ExternalRef,
            Data = new AccountHolderDto(
                dto.AddedOn ?? DateTime.UtcNow,
                dto.Username,
                dto.Accounts),
            HasUnpublishedEvents = dto.HasUnpublishedEvents,
            UnpublishedEventsJson = dto.UnpublishedEvents?.Any() ?? false ?
                JsonSerializer.Serialize(Enumerable.Cast<object>(dto.UnpublishedEvents)) :
                null
        };
    }
}