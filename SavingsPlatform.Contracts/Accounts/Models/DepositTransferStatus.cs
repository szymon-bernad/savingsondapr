using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SavingsPlatform.Contracts.Accounts.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DepositTransferStatus
    {
        New,
        DebtorDebited,
        BeneficiaryCredited,
        Completed
    }
}
