using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace UndergroundIRO.ApiBase.Services
{
    public static class LoggerFactory
    {
        public static ILoggerFactory CreateDefaultLoggerFactory()
        {
            var services = new ServiceCollection();
            services.AddLogging((conf) =>
            {
                conf.AddDebug();
            });
            var sp = services.BuildServiceProvider();
            return sp.GetRequiredService<ILoggerFactory>();
        }
    }
}
