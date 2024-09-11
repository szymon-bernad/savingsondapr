using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavingsPlatform.Common.Interfaces
{
    public interface IAggregateRoot<T> where T : IAggregateStateEntry
    {
        public T? State { get; }

    }
}
