using Marten.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavingsPlatform.Common.Repositories
{
    public class AggregateState<T> : IAggregateState
    {
        public string Id { get; set; } = string.Empty;
        public T? Data { get; set; }
        public bool HasUnpublishedEvents { get; set; } = false;
        public string? UnpublishedEventsJson { get; set; } = default;

        [Version]
        public Guid Version { get; set; } = Guid.Empty;
        public string? ETag { get; set; } = null;
    }
}
