using SavingsPlatform.Contracts.Accounts.Requests;

namespace Dashboard.Api.ApiClients;

public interface IExchangeApiClient
{
    public Task ScheduleExchangeOrderAsync(CurrencyExchangeOrder order);
}
