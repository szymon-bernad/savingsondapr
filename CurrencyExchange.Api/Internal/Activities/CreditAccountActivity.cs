using CurrencyExchange.Api.ApiClients;
using Dapr.Workflow;
using SavingsPlatform.Contracts.Accounts.Requests;

namespace CurrencyExchange.Api.Internal.Activities;

public class CreditAccountActivity(IAccountsApiClient accountsApiClient)
    : WorkflowActivity<CreditAccount, AccountActivityResult>
{
    private readonly IAccountsApiClient _accountsApiClient = accountsApiClient;

    public override async Task<AccountActivityResult> RunAsync(WorkflowActivityContext context, CreditAccount input)
    {
        try
        {
            await _accountsApiClient.CreditAccountAsync(input);
        }
        catch (Exception ex)
        {
            return new AccountActivityResult(false, $"{ex}", true);
        }

        return new AccountActivityResult(true);
    }
}
