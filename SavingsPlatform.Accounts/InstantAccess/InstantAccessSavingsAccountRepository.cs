using Marten;
using Microsoft.Extensions.Logging;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Common.Repositories;
using SavingsPlatform.Common.Repositories.Marten;
using SavingsPlatform.Common.Services;

namespace SavingsPlatform.Accounts.Aggregates.InstantAccess
{
    internal class InstantAccessSavingsAccountRepository : MartenStateEntryRepositoryBase<InstantAccessSavingsAccountState, InstantAccessSavingsAccountDto>
    {
        public InstantAccessSavingsAccountRepository(
            IDocumentSession docSession,
            IStateMapper<AggregateState<InstantAccessSavingsAccountDto>, InstantAccessSavingsAccountState> mapper,
            IEventPublishingService eventPublishingService,
            ILogger<MartenStateEntryRepositoryBase<InstantAccessSavingsAccountState, InstantAccessSavingsAccountDto>> logger)
            : base(docSession, mapper, eventPublishingService, logger)
        {
        }
    }
}
