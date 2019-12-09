using System;
using System.Threading.Tasks;

namespace UndergroundIRO.ApiBase.Services
{
    public interface ITimeoutQueue
    {
        TimeSpan Timeout { get; set; }

        /// <summary>
        /// For each delegate in TimeoutQueue with position bigger than this value will be throwed exception.
        /// </summary>
        int QueuePendingItemLimit { get; set; }

        Task Execute(Func<Task> func);
    }
}