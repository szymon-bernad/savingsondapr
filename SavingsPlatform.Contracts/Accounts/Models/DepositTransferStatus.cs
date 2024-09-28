using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavingsPlatform.Contracts.Accounts.Models
{
    public enum DepositTransferStatus
    {
        New,
        DebtorDebited,
        BeneficiaryCredited,
        Completed
    }
}
