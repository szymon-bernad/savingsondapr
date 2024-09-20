using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavingsPlatform.Common.Repositories
{
    public interface IAggregateState
    {
        bool HasUnpublishedEvents { get; set; }
        string? UnpublishedEventsJson { get; set; }
        Guid Version { get; set; }
    }
}
