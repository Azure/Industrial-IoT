// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Logging
{
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Console logging helpers ported from the former Legacy.Extensions.Logging
    /// <c>Log</c> helper used to bootstrap loggers outside of the host DI
    /// container (cli programs and test fixtures).
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// Console logger
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="level"></param>
        public static ILogger<T> Console<T>(LogLevel? level = null)
        {
            using var factory = ConsoleFactory(level);
            return factory.CreateLogger<T>();
        }

        /// <summary>
        /// Console logger
        /// </summary>
        /// <param name="name"></param>
        /// <param name="level"></param>
        public static ILogger Console(string name, LogLevel? level = null)
        {
            using var factory = ConsoleFactory(level);
            return factory.CreateLogger(name);
        }

        /// <summary>
        /// Create logger factory
        /// </summary>
        /// <param name="level"></param>
        public static ILoggerFactory ConsoleFactory(LogLevel? level = null)
        {
            level ??=
#if DEBUG
                LogLevel.Debug;
#else
                LogLevel.Information;
#endif
            return LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(level.Value);
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.SingleLine = true;
                });
            });
        }
    }
}
