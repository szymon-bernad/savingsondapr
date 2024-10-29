using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Contracts.Accounts.Requests;

public record CurrencyExchangeQuery(Currency Source, Currency Target, decimal Amount);
