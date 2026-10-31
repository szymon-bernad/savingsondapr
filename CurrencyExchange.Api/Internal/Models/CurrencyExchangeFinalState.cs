using CurrencyExchange.Api.Internal.Activities;
using SavingsPlatform.Contracts.Accounts.Models;
using SavingsPlatform.Contracts.Accounts.Requests;

namespace CurrencyExchange.Api.Internal.Models
{
    public class CurrencyExchangeFinalState
    {
        public required CurrencyExchangeOrder Order { get; init; }

        public required ExchangeResult Result { get; init; }

    }
}
