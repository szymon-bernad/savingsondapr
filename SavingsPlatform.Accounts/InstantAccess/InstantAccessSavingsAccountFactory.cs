using Microsoft.Extensions.Options;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Current;
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
                return new InstantAccessSavingsAccount(_repository, _simulationConfig, null);
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
            var stateEntry = (await _repository.QueryAccountsByKeyAsync(["externalRef"], [externalRef])).SingleOrDefault();
            
            if (stateEntry is not null)
            {
                return new InstantAccessSavingsAccount(_repository, _simulationConfig, stateEntry);
            }
            else throw new InvalidOperationException($"Cannot get instance with externalRef = {externalRef}");
        }

        public async Task<InstantAccessSavingsAccount?> TryGetInstanceAsync(string id)
        {
            if (string.IsNullOrEmpty(id?.Trim()))
            {
                throw new InvalidOperationException($"{nameof(id)} cannot be null or empty");
            }

            var result = await _repository.GetAccountAsync(id);

            return result is not null ?
                new InstantAccessSavingsAccount(_repository, _simulationConfig, result) :
                null;
        }

        public Task<InstantAccessSavingsAccount?> TryGetInstanceByExternalRefAsync(string externalRef)
        {
            throw new NotImplementedException();
        }
    }
}
