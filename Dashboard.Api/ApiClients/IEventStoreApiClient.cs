using SavingsPlatform.Contracts.Accounts.Requests;
using SavingsPlatform.Contracts.CurrencyExchange.Response;

namespace Dashboard.Api.ApiClients;

public interface IEventStoreApiClient
{
    Task InitiateCurrencyExchangeSummaryAsync(CurrencyExchangeSummaryRequest request);

    Task<CurrencyExchangeSummaryResponse> FetchCurrencyExchangeSummaryAsync(CurrencyExchangeSummaryRequest request);
}
