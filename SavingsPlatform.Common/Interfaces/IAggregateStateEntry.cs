using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavingsPlatform.Common.Interfaces
{
    public interface IAggregateStateEntry : IEntry
    {
        public string ExternalRef { get; init; }

        public bool HasUnpublishedEvents { get; set; }

        ICollection<object>? UnpublishedEvents { get; set; }
    }
}
