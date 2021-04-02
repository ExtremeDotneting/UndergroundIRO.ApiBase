using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using UndergroundIRO.ApiBase.Services;

namespace UndergroundIRO.ApiBase
{
    public class HttpConfiguration
    {
        public virtual bool UseTimeoutQueue { get; set; } = false;

        /// <summary>
        /// Gets or sets the default header.
        /// </summary>
        public virtual IDictionary<string, string> DefaultHeaders { get; set; } = new Dictionary<string, string>();

        public LogLevel LogLevel { get; set; } = LogLevel.Information;
    }
}
