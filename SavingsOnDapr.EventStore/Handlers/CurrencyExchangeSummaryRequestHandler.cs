using JasperFx.Core;
using Microsoft.Extensions.Caching.Distributed;
using SavingsOnDapr.EventStore.Store;
using SavingsPlatform.Contracts.Accounts.Enums;
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

        var summaryTasks = new List<SummaryTaskData> { new SummaryTaskData(request.SourceCurrency, request.TargetCurrency, request.StartDate) };

        if (request.EndDate > request.StartDate)
        {
            var itDate = request.StartDate.AddDays(1);
            while (itDate <= request.EndDate)
            {
                summaryTasks.Add(new SummaryTaskData(request.SourceCurrency, request.TargetCurrency, itDate));
                itDate = itDate.AddDays(1);
            }
        }

        var results = await Task.WhenAll(
            summaryTasks.Select(async st => {
                var res = await _store.GetSummary($"{st.Source}=>{st.Target}_{st.Date:yyyy-MM-dd}");
                res ??= new Aggregations.CurrencyExchangeSummaryDto
                {
                    SummaryDate = new DateOnly(st.Date.Year, st.Date.Month, st.Date.Day),
                    SourceCurrency = st.Source,
                    TargetCurrency = st.Target,
                    TotalCountOfExchanges = 0,
                    TotalSourceAmountOfExchanges = 0,
                    TotalTargetAmountOfExchanges = 0,
                    Entries = []
                };
                return res;
            }));

        var entries = results.Where(r => r is not null)
            .Select(r => new CurrencyExchangeSummaryValueEntry 
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

    internal record SummaryTaskData(Currency Source, Currency Target, DateOnly Date);
}
