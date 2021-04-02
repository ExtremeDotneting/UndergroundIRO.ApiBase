using System;
using System.Threading;
using System.Threading.Tasks;
using NeoSmart.AsyncLock;

namespace UndergroundIRO.ApiBase.Services
{
    /// <summary>
    /// Made with NeoSmart.AsyncLock
    /// </summary>
    public class TimeoutQueue : ITimeoutQueue
    {
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMilliseconds(500);

        readonly AsyncLock _lock = new AsyncLock();

        public async Task Execute(Func<Task> func)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));
            using (await _lock.LockAsync())
            {
                await func();
                await Task.Delay(Timeout);
            }
        }
    }
}