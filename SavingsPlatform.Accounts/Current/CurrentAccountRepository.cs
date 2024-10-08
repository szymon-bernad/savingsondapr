﻿using Marten;
using Microsoft.Extensions.Logging;
using SavingsPlatform.Accounts.Current.Models;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Common.Repositories;
using SavingsPlatform.Common.Repositories.Marten;
using SavingsPlatform.Common.Services;

namespace SavingsPlatform.Accounts.Current;

internal class CurrentAccountRepository : MartenStateEntryRepositoryBase<CurrentAccountState, CurrentAccountDto>
{
    public CurrentAccountRepository(
        ISessionFactory docSessionFactory,
        IStateMapper<AggregateState<CurrentAccountDto>, CurrentAccountState> mapper,
        IEventPublishingService eventPublishingService,
        ILogger<MartenStateEntryRepositoryBase<CurrentAccountState, CurrentAccountDto>> logger)
        : base(docSessionFactory, mapper, eventPublishingService, logger)
    { }
}

