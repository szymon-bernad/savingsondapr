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
using SavingsPlatform.Accounts.AccountHolders;
using SavingsPlatform.Accounts.Handlers;

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
            .AddTransient<IStateMapper<AggregateState<AccountHolderDto>, AccountHolderState>, AccountHolderMapper>()
            .AddScoped<IStateEntryRepository<AccountHolderState>, AccountHolderRepository>()
            .AddScoped<IStateEntryQueryHandler<AccountHolderState>, AccountHolderRepository>()
            .AddScoped<IAccountsQueryHandler, AccountsQueryHandler>()
            .AddTransient<IAggregateRootFactory<AccountHolder, AccountHolderState>, AccountHolderFactory>()
            .AddActors(options =>
                {
                    options.Actors.RegisterActor<DepositTransferActor>();
                    options.Actors.RegisterActor<AccountCreationActor>();
                });

        services.Configure<EventStoreApiConfig>(config.GetSection("EventStoreApiConfig"));
        services.Configure<ServiceConfig>(config.GetSection("ServiceConfig"));

        return services;
    }
}
