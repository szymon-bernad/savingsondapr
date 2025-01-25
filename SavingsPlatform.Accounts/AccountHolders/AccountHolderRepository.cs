using Marten;
using Microsoft.Extensions.Logging;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Common.Repositories;
using SavingsPlatform.Common.Repositories.Marten;
using SavingsPlatform.Common.Services;

namespace SavingsPlatform.Accounts.AccountHolders;

internal class AccountHolderRepository : MartenStateEntryRepositoryBase<AccountHolderState, AccountHolderDto>
{
    public AccountHolderRepository(
        ISessionFactory docSessionFactory,
        IStateMapper<AggregateState<AccountHolderDto>, AccountHolderState> mapper,
        IEventPublishingService eventPublishingService,
        ILogger<MartenStateEntryRepositoryBase<AccountHolderState, AccountHolderDto>> logger)
            : base(docSessionFactory, mapper, eventPublishingService, logger)
    { }
}