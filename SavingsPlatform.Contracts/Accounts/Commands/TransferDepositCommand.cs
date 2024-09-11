using MediatR;
using SavingsPlatform.Contracts.Accounts.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavingsPlatform.Contracts.Accounts.Commands
{
    public record TransferDepositCommand(
        string SavingsAccountRef,
        DateTime TransactionDate,
        decimal Amount,
        TransferDirection Direction,
        string? TransferId,
        bool WaitForAccountCreation = false
        ) : IRequest;
}
