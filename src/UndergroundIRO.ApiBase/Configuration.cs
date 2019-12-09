﻿using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using UndergroundIRO.ApiBase.Services;

namespace UndergroundIRO.ApiBase
{
    public class Configuration
    {
        public virtual string BasePath { get; set; }

        public virtual bool UseTimeoutQueue { get; set; } = false;

        public virtual ITimeoutQueue TimeoutQueue { get; set; } = new TimeoutQueue();

        public virtual IExceptionsFactory ExceptionsFactory { get; set; } = new ExceptionsFactory();

        /// <summary>
        /// Gets or sets the default header.
        /// </summary>
        public virtual IDictionary<string, string> DefaultHeaders { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets the HTTP timeout (milliseconds) of ApiClient. Default to 100000 milliseconds.
        /// </summary>
        public virtual TimeSpan ResponseTimeout { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the HTTP user agent.
        /// </summary>
        /// <value>Http user agent.</value>
        public virtual string UserAgent { get; set; }

        /// <summary>
        /// By default - DebugLoggerFactory.
        /// </summary>
        public ILoggerFactory LoggerFactory { get; set; } = Services.LoggerFactory.CreateDefaultLoggerFactory();

        public LogLevel LogLevel { get; set; } = LogLevel.Information;
    }
}
