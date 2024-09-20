using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavingsPlatform.Contracts.Accounts.Models
{
    public record PlatformMappingEntry(string PlatformId, string SettlementRef, ICollection<string> AccountRefs);
}
