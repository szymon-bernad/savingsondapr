
using System.Runtime.CompilerServices;

namespace SavingsPlatform.Common.Helpers
{
    public class ThreadSynchronizer : IThreadSynchronizer
    {
        private readonly Dictionary<string, WeakReference<SemaphoreSlim>> SemaphoreDict = new ();
        private readonly object _lock = new();
        public async Task ExecuteSynchronizedAsync(string key, Func<Task> action)
        {
            var keySemaphore = default(SemaphoreSlim);

            try
            {
                lock (_lock)
                {
                    if (!SemaphoreDict.ContainsKey(key))
                    {
                        keySemaphore = new SemaphoreSlim(1, 1);
                        SemaphoreDict.Add(key, new WeakReference<SemaphoreSlim>(keySemaphore));
                    }
                    else
                    {
                        var value = SemaphoreDict[key];
                        if (!value.TryGetTarget(out keySemaphore))
                        {
                            keySemaphore = new SemaphoreSlim(1, 1);
                            SemaphoreDict[key] = new WeakReference<SemaphoreSlim>(keySemaphore);
                        }
                    }
                }
            
                await keySemaphore.WaitAsync();

                var task = action();
                await task;
            }
            finally
            {
                keySemaphore?.Release();
            }
        }
    }
}
