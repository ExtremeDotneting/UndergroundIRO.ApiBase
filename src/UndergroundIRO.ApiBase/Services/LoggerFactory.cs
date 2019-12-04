using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApiBase.Services
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
