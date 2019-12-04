using ApiBase.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using System;
using System.Collections.Generic;

namespace ApiBase
{
    public class WebsocketConfiguration
    {
        public virtual string BasePath { get; set; }

        /// <summary>
        /// By default - DebugLoggerFactory.
        /// </summary>
        public ILoggerFactory LoggerFactory { get; set; } = Services.LoggerFactory.CreateDefaultLoggerFactory();

        public LogLevel LogLevel { get; set; } = LogLevel.Information;

        public virtual int ErrorConnectionTimeoutMS { get; set; } = 5000;
    }
}
