using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Contracts.Accounts.Requests;

public record CurrencyExchangeBaseRateRequest(Currency Source, Currency Target, decimal BaseRate);

