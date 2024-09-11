using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavingsPlatform.Contracts.Accounts.Models
{
    public record InterestAccrualData
    {
        public required string AccountKey { get; init; } = string.Empty;

        public DateTime? LastAccrualDate { get; init; }
    }
}
