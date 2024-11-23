using CurrencyExchange.Api.ApiClients;
using Dapr.Workflow;
using SavingsPlatform.Contracts.Accounts.Requests;

namespace CurrencyExchange.Api.Internal.Activities;

public class DebitAccountActivity(IAccountsApiClient accountsApiClient)
    : AccountActivityBase<DebitAccount>
{
    private readonly IAccountsApiClient _accountsApiClient = accountsApiClient;

    public override Task<AccountActivityResult> RunAsync(WorkflowActivityContext context, DebitAccount input)
        => RunWithRetries(async () => await _accountsApiClient.DebitAccountAsync(input));
}
