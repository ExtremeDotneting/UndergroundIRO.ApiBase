using System;
using System.Threading.Tasks;

namespace ApiBase.Services
{
    public static class TimeoutQueueExtensions
    {
        public static async Task<TResult> Execute<TResult>(this ITimeoutQueue @this, Func<Task<TResult>> func)
        {
            var res = default(TResult);
            await @this.Execute(async () =>
            {
                res = await func();
            });
            return res;
        }
    }
}