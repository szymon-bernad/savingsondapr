using SavingsPlatform.Contracts.Accounts.Requests;

namespace CurrencyExchange.Api.ApiClients;

public interface IAccountsApiClient
{
    Task DebitAccountAsync(DebitAccount request);
    Task CreditAccountAsync(CreditAccount request);
}
