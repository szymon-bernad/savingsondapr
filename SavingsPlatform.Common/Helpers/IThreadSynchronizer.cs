using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavingsPlatform.Common.Helpers
{
    public interface IThreadSynchronizer
    {
        Task ExecuteSynchronizedAsync(string key, Func<Task> action);
    }
}
