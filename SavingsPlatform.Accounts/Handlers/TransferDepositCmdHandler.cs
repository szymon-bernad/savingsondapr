using Dapr.Actors.Client;
using MediatR;
using SavingsPlatform.Accounts.Actors;
using SavingsPlatform.Accounts.Aggregates.InstantAccess;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Commands;
using SavingsPlatform.Contracts.Accounts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavingsPlatform.Accounts.Handlers
{
    public class TransferDepositCmdHandler : IRequestHandler<TransferDepositCommand>
    {
        private readonly IActorProxyFactory _actorProxyFactory;
        private readonly IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState> _aggregateFactory;
        
        public TransferDepositCmdHandler(
            IActorProxyFactory proxyFactory,
             IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState> aggregateFactory)
        {
            _actorProxyFactory = proxyFactory;
            _aggregateFactory = aggregateFactory;
        }

        public async Task Handle(TransferDepositCommand request, CancellationToken cancellationToken)
        {
            var account = await _aggregateFactory.GetInstanceByExternalRefAsync(request.SavingsAccountRef);
            if (account.State == null)
            {
                throw new InvalidOperationException("Account not found");
            }

            var debtorId = request.Direction switch
            { 
                Contracts.Accounts.Enums.TransferType.SavingsToCurrent => account.State.Key,
                Contracts.Accounts.Enums.TransferType.CurrentToSavings => account.State.CurrentAccountId,
                _ => throw new InvalidOperationException("Unsupported transfer type") 
            };
            var beneficiaryId = request.Direction switch
            {
                Contracts.Accounts.Enums.TransferType.SavingsToCurrent => account.State.CurrentAccountId,
                Contracts.Accounts.Enums.TransferType.CurrentToSavings => account.State.Key,
                _ => throw new InvalidOperationException("Unsupported transfer type")
            };

            var transferData = new DepositTransferData 
            { 
                Amount = request.Amount,
                TransferId = request.TransferId,
                Direction = request.Direction,
                DebtorAccountId = debtorId,
                BeneficiaryAccountId = beneficiaryId
            };

            var actorInstance = _actorProxyFactory.CreateActorProxy<IDepositTransferActor>(new Dapr.Actors.ActorId(request.TransferId), nameof(DepositTransferActor));

            await actorInstance.InitiateTransferAsync(transferData);
        }
    }
}
