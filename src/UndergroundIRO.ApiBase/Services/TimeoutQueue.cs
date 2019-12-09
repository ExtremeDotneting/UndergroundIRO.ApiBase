using System;
using System.Threading;
using System.Threading.Tasks;

namespace UndergroundIRO.ApiBase.Services
{
    public class TimeoutQueue : ITimeoutQueue
    {
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// For each delegate in TimeoutQueue with position bigger than this value will be throwed exception.
        /// </summary>
        public int QueuePendingItemLimit { get; set; } = 10;

        SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public async Task Execute(Func<Task> func)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));
            if (_semaphoreSlim.CurrentCount > QueuePendingItemLimit)
            {
                throw new Exception("Expected TimeoutQueue limit exceeded.");
            }
            await _semaphoreSlim.WaitAsync();
            try
            {
                await func();
            }
            finally
            {
                await Task.Delay(Timeout);
                _semaphoreSlim.Release();
            }

        }
    }
}