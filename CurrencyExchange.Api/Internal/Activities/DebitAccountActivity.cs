using CurrencyExchange.Api.ApiClients;
using Dapr.Workflow;
using SavingsPlatform.Contracts.Accounts.Requests;

namespace CurrencyExchange.Api.Internal.Activities;

public class DebitAccountActivity(IAccountsApiClient accountsApiClient)
    : WorkflowActivity<DebitAccount, AccountActivityResult>
{
    private readonly IAccountsApiClient _accountsApiClient = accountsApiClient;

    public override async Task<AccountActivityResult> RunAsync(WorkflowActivityContext context, DebitAccount input)
    {
        try { 
            await _accountsApiClient.DebitAccountAsync(input);
        }
        catch (Exception ex)
        {
            return new AccountActivityResult(false, $"{ex}", true);
        }

        return new AccountActivityResult(true);
    }
}
