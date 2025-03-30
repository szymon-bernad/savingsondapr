using JasperFx.Core;
using Microsoft.Extensions.Caching.Distributed;
using SavingsOnDapr.EventStore.Store;
using SavingsPlatform.Contracts.Accounts.Requests;
using SavingsPlatform.Contracts.CurrencyExchange.Response;
using System.Text.Json;
using static FastExpressionCompiler.ImTools.SmallMap4;

namespace SavingsOnDapr.EventStore.Handlers;
public class CurrencyExchangeSummaryRequestHandler(
    CurrencyExchangeEventStore _store,
    IDistributedCache _cache)
    : ICurrencyExchangeSummaryRequestHandler
{
    public async Task Handle(CurrencyExchangeSummaryRequest request)
    {

        ICollection<string> streamIds = new List<string> { $"{request.SourceCurrency}=>{request.TargetCurrency}_{request.StartDate:yyyy-MM-dd}" };

        if (request.EndDate > request.StartDate)
        {
            var itDate = request.StartDate.AddDays(1);
            while (itDate <= request.EndDate)
            {
                streamIds.Add($"{request.SourceCurrency}=>{request.TargetCurrency}_{itDate:yyyy-MM-dd}");
                itDate = itDate.AddDays(1);
            }
        }

        var results = await Task.WhenAll(streamIds.Select(streamId => _store.GetSummary(streamId)));
        var entries = results.Where(r => r is not null).Select(r => new CurrencyExchangeSummaryValueEntry 
        {
            EntryName = $"{request.SourceCurrency}=>{request.TargetCurrency}_{r.SummaryDate:yyyy-MM-dd}",
            ColumnValues = 
            [
                    $"{r.SummaryDate}", 
                    $"{r.TotalCountOfExchanges}", 
                    $"{r.TotalSourceAmountOfExchanges}", 
                    $"{r.TotalTargetAmountOfExchanges}" 
            ], 
        });
        var response = new CurrencyExchangeSummaryResponse
        {
            ResponseKey = $"{request.SourceCurrency}=>{request.TargetCurrency}_{request.StartDate:yyyy-MM-dd}_{request.EndDate:yyyy-MM-dd}",
            ColumnNames = ["Date", "TotalExchangesCount", "TotalSourceAmount", "TotalTargetAmount"],
            Entries = entries.ToArray(),
        };

        await _cache.SetStringAsync(response.ResponseKey, JsonSerializer.Serialize(response), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60)
        });
    }
}
