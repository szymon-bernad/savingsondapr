using CurrencyExchange.Api.ApiClients;
using Dapr.Workflow;
using SavingsPlatform.Contracts.Accounts.Requests;

namespace CurrencyExchange.Api.Internal.Activities;

public class CreditAccountActivity(IAccountsApiClient accountsApiClient)
    : AccountActivityBase<CreditAccount>
{
    private readonly IAccountsApiClient _accountsApiClient = accountsApiClient;

    public override Task<AccountActivityResult> RunAsync(WorkflowActivityContext context, CreditAccount input)
        => RunWithRetries(async () => await _accountsApiClient.CreditAccountAsync(input));
}
