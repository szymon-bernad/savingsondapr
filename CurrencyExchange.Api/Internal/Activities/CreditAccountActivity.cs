using CurrencyExchange.Api.ApiClients;
using Dapr.Workflow;
using SavingsPlatform.Contracts.Accounts.Requests;

namespace CurrencyExchange.Api.Internal.Activities;

public class CreditAccountActivity(IAccountsApiClient accountsApiClient)
    : WorkflowActivity<CreditAccount, bool>
{
    private readonly IAccountsApiClient _accountsApiClient = accountsApiClient;

    public override async Task<bool> RunAsync(WorkflowActivityContext context, CreditAccount input)
    {
        await _accountsApiClient.CreditAccountAsync(input);

        return true;
    }
}
