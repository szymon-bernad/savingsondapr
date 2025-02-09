using SavingsPlatform.Contracts.Accounts.Models;
using SavingsPlatform.Contracts.Accounts.Requests;

namespace Dashboard.Api.ApiClients;

public interface IExchangeApiClient
{
    public Task ScheduleExchangeOrderAsync(CurrencyExchangeOrder order);

    public Task<CurrencyExchangeResponse> GetExchangeRateAsync(CurrencyExchangeQuery query);
}
