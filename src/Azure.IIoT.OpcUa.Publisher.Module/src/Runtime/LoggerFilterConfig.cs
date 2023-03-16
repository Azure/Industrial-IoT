// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Runtime
{
    using Furly.Extensions.Configuration;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using System;

    /// <summary>
    /// Configure logger factory
    /// </summary>
    public sealed class LoggerFilterConfig : ConfigureOptionBase<LoggerFilterOptions>
    {
        /// <summary>
        /// Configuration
        /// </summary>
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public const string LogLevelKey = "LogLevel";
        public const LogLevel LogLevelDefault = LogLevel.Information;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <inheritdoc/>
        public override void Configure(string name, LoggerFilterOptions options)
        {
            if (Enum.TryParse<LogLevel>(GetStringOrDefault(LogLevelKey), out var logLevel))
            {
                options.MinLevel = logLevel;
            }
        }

        /// <summary>
        /// Create logging configurator
        /// </summary>
        /// <param name="configuration"></param>
        public LoggerFilterConfig(IConfiguration configuration) : base(configuration)
        {
        }
    }
}
