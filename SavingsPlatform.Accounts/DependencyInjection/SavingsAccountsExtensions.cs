using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Aggregates.InstantAccess;
using SavingsPlatform.Accounts.Config;
using SavingsPlatform.Common.Config;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Common.Repositories;
using SavingsPlatform.Common.Services;
using SavingsPlatform.Common.Helpers;

namespace SavingsPlatform.Accounts.DependencyInjection;

public static class SavingsPlatformAccountsDIExt
{
    public static IServiceCollection AddSavingsAccounts(
        this IServiceCollection services,
        ConfigurationManager configuration,
        int daprPort)
    {
        services.AddTransient<IStateMapper<AggregateState<InstantAccessSavingsAccountDto>, InstantAccessSavingsAccountState>, InstantAccessSavingsAccountStateMapper>();
        services.AddScoped<IStateEntryRepository<InstantAccessSavingsAccountState>, InstantAccessSavingsAccountRepository>();
        services.AddTransient<IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState>, InstantAccessSavingsAccountFactory>();
        services.AddTransient<IEventPublishingService, DaprEventPublishingService>();
        services.AddSingleton<IThreadSynchronizer, ThreadSynchronizer>();

        return services;
    }
}
