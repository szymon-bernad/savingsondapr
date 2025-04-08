using SavingsPlatform.Contracts.Accounts.Requests;

namespace SavingsOnDapr.EventStore.Handlers;

public interface ICurrencyExchangeSummaryRequestHandler
{
    Task Handle(CurrencyExchangeSummaryRequest request);
}
