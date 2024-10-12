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
using SavingsPlatform.Accounts.Current.Models;
using SavingsPlatform.Accounts.Current;
using SavingsPlatform.Accounts.Actors;
using SavingsPlatform.Accounts.ApiClients;

namespace SavingsPlatform.Accounts.DependencyInjection;

public static class SavingsPlatformAccountsDIExt
{
    public static IServiceCollection AddSavingsAccounts(this IServiceCollection services, ConfigurationManager config)
    {
        services.AddTransient<IStateMapper<AggregateState<InstantAccessSavingsAccountDto>, InstantAccessSavingsAccountState>, InstantAccessSavingsAccountStateMapper>()
            .AddScoped<IStateEntryRepository<InstantAccessSavingsAccountState>, InstantAccessSavingsAccountRepository>()
            .AddScoped<IStateEntryQueryHandler<InstantAccessSavingsAccountState>, InstantAccessSavingsAccountRepository>()
            .AddTransient<IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState>, InstantAccessSavingsAccountFactory>()
            .AddTransient<IStateMapper<AggregateState<CurrentAccountDto>, CurrentAccountState>, CurrentAccountStateMapper>()
            .AddScoped<IStateEntryRepository<CurrentAccountState>, CurrentAccountRepository>()
            .AddScoped<IStateEntryQueryHandler<CurrentAccountState>, CurrentAccountRepository>()
            .AddTransient<IAggregateRootFactory<CurrentAccount, CurrentAccountState>, CurrentAccountFactory>()
            .AddScoped<IEventPublishingService, DaprEventPublishingService>()
            .AddScoped<IEventStoreApiClient, EventStoreApiClient>()
            .AddSingleton<IThreadSynchronizer, ThreadSynchronizer>()
            .AddActors(options =>
                {
                    options.Actors.RegisterActor<DepositTransferActor>();
                });

        services.Configure<EventStoreApiConfig>(config.GetSection("EventStoreApiConfig"));

        return services;
    }
}
