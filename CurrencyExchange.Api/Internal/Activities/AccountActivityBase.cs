using Dapr.Workflow;

namespace CurrencyExchange.Api.Internal.Activities
{
    public abstract class AccountActivityBase<TCommand> : WorkflowActivity<TCommand, AccountActivityResult>
    {
        protected int _activityRetriesCount = 3;
        protected int _retryDelayMs = 100;

        protected AccountActivityBase(
            int activityRetriesCount = 3,
            int retryDelayMs = 100)
        {
            _activityRetriesCount = activityRetriesCount;
            _retryDelayMs = retryDelayMs;
        }

        protected async Task<AccountActivityResult> RunWithRetries(
            Func<Task> action)
        {
            int tries = 0;

            var result = await RunAction(action);

            while(tries < _activityRetriesCount && !result.Succeeded && result.IsRetriable)
            {
                await Task.Delay(_retryDelayMs);

                result = await RunAction(action);
                ++tries;
            }

            if (!result.Succeeded)
            {
                result = result with
                {
                    Reason = $"Activity failed after {tries} retries",
                };
            }

            return result;
        }

        protected async Task<AccountActivityResult> RunAction(Func<Task> action)
        {
            try
            {
                await action();
                return new AccountActivityResult(true);
            }
            catch (Exception ex)
            {
                return new AccountActivityResult(false, ex.Message, true);
            }
        }
    }
}
