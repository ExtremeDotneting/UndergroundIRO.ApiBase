using System;
using System.Threading.Tasks;

namespace UndergroundIRO.ApiBase.Services
{
    public interface ITimeoutQueue
    {
        TimeSpan Timeout { get; set; }

        Task Execute(Func<Task> func);
    }
}