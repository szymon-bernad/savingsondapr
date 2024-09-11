using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavingsPlatform.Common.Repositories
{
    public interface IAggregateState
    {
        public bool HasUnpublishedEvents { get; set; }
        public string? UnpublishedEventsJson { get; set; }

        public Guid Version { get; set; }

        public string? ETag { get; set; }
    }
}
