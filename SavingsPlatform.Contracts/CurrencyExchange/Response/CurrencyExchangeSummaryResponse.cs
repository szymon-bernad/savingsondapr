using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavingsPlatform.Contracts.CurrencyExchange.Response;

public record CurrencyExchangeSummaryResponse
{
    public string ResponseKey { get; init; } = string.Empty;

    public string[] ColumnNames { get; init; } = [];

    public IDictionary<string, string[]>? ColumnValues { get; init; }
}
