﻿using Microsoft.Extensions.Options;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Common.Config;
using SavingsPlatform.Common.Interfaces;

namespace SavingsPlatform.Accounts.Aggregates.InstantAccess
{
    internal class InstantAccessSavingsAccountFactory : IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState>
    {
        private readonly IStateEntryRepository<InstantAccessSavingsAccountState> _repository;
        private readonly SimulationConfig _simulationConfig;

        public InstantAccessSavingsAccountFactory(
            IStateEntryRepository<InstantAccessSavingsAccountState> repo,
            IOptions<SimulationConfig> simulationConfig)
        {
            _repository = repo;
            _simulationConfig = simulationConfig?.Value ?? new SimulationConfig { SpeedMultiplier = 1 };
        }

        public async Task<InstantAccessSavingsAccount> GetInstanceAsync(string? id = null)
        {
            if (id is null)
            {
                return new InstantAccessSavingsAccount(_repository, null);
            }

            var stateEntry = await _repository.GetAccountAsync(id);

            if (stateEntry is null)
            {
                throw new InvalidOperationException($"Account with {id} not found");
            }
            return new InstantAccessSavingsAccount(_repository, _simulationConfig, stateEntry);
        }

        public async Task<InstantAccessSavingsAccount> GetInstanceByExternalRefAsync(string externalRef)
        {
            var stateEntry = (await _repository.QueryAccountsByKeyAsync(new string[] { "data.externalRef" }, new string[] { externalRef })).SingleOrDefault();
            if (stateEntry is not null)
            {
                return new InstantAccessSavingsAccount(_repository, _simulationConfig, stateEntry);
            }
            else throw new InvalidOperationException($"Cannot get instance with externalRef = {externalRef}");
        }
    }
}
