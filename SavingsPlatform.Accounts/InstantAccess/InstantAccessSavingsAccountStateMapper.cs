using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Common.Repositories;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SavingsPlatform.Accounts.Aggregates.InstantAccess;

public class InstantAccessSavingsAccountStateMapper : IStateMapper<AggregateState<InstantAccessSavingsAccountDto>, InstantAccessSavingsAccountState>
{
    public InstantAccessSavingsAccountState Map(AggregateState<InstantAccessSavingsAccountDto> state)
    {
        var events = !string.IsNullOrEmpty(state.UnpublishedEventsJson) ?
            JsonSerializer.Deserialize<IEnumerable<JsonNode>>(state!.UnpublishedEventsJson) : Enumerable.Empty<JsonNode>();

        var unpubEvents = events is not null ? Enumerable.Cast<object>(events.Select(e => e.AsObject())).ToList() : null;

        return new InstantAccessSavingsAccountState
        {
            Key = state.Data!.Id,
            ExternalRef = state.Data!.ExternalRef,
            InterestRate = state.Data!.InterestRate,
            AccruedInterest = state.Data!.AccruedInterest,
            OpenedOn = state.Data.OpenedOn,
            ActivatedOn = state.Data.ActivatedOn,
            TotalBalance = state.Data!.TotalBalance,
            CurrentAccountId = state.Data.CurrentAccountId,
            HasUnpublishedEvents = state.HasUnpublishedEvents,
            InterestApplicationDueOn = state.Data.InterestApplicationDueOn,
            InterestAccrualDueOn = state.Data.InterestAccrualDueOn,
            UnpublishedEvents = unpubEvents
        };
    }

    public AggregateState<InstantAccessSavingsAccountDto> ReverseMap(InstantAccessSavingsAccountState dto)
    {
        return new AggregateState<InstantAccessSavingsAccountDto>
        {
            Id = dto.Key,
            Data = new InstantAccessSavingsAccountDto(
                dto.Key,
                dto.ExternalRef,
                dto.OpenedOn,
                dto.ActivatedOn,
                dto.InterestRate,
                dto.TotalBalance,
                dto.AccruedInterest,
                dto.CurrentAccountId,
                dto.InterestApplicationFrequency,
                dto.InterestApplicationDueOn,
                dto.InterestAccrualDueOn),
            HasUnpublishedEvents = dto.HasUnpublishedEvents,
            UnpublishedEventsJson = dto.UnpublishedEvents?.Any() ?? false ?
                JsonSerializer.Serialize(Enumerable.Cast<object>(dto.UnpublishedEvents)) : null
        };
    }
}
