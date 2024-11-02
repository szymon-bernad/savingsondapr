using CurrencyExchange.Api.ApiClients;
using Dapr.Workflow;
using SavingsPlatform.Contracts.Accounts.Requests;

namespace CurrencyExchange.Api.Internal.Activities;

public class DebitAccountActivity(IAccountsApiClient accountsApiClient)
    : WorkflowActivity<DebitAccount, bool>
{
    private readonly IAccountsApiClient _accountsApiClient = accountsApiClient;

    public override async Task<bool> RunAsync(WorkflowActivityContext context, DebitAccount input)
    {
        await _accountsApiClient.DebitAccountAsync(input);

        return true;
    }
}
