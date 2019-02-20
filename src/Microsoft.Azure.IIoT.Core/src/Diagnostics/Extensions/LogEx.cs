// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Serilog {
    using Serilog.Events;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Serilog extensions
    /// </summary>
    public static class LogEx {
        private const string kDefaultTemplate =
            "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

        /// <summary>
        /// Create web logger
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="config"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static LoggerConfiguration Console(this LoggerConfiguration configuration,
            IConfiguration config = null, LogEventLevel level = LogEventLevel.Debug) {
            if (config != null) {
                configuration = configuration.ReadFrom.Configuration(config);
            }
            return configuration
                .Enrich.WithProperty("SourceContext", null)
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: kDefaultTemplate)
                .MinimumLevel.Is(level);
        }

        /// <summary>
        /// Create standard console logger
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public static ILogger Console(LogEventLevel level = LogEventLevel.Debug) =>
            new LoggerConfiguration().Console(null, level).CreateLogger();

        /// <summary>
        /// Create console logger
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static LoggerConfiguration ConsoleOut(this LoggerConfiguration configuration,
            LogEventLevel level = LogEventLevel.Debug) {
            return configuration
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: "{Message:lj}{NewLine}")
                .MinimumLevel.Is(level);
        }

        /// <summary>
        /// Create console out like logger
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public static ILogger ConsoleOut(LogEventLevel level = LogEventLevel.Debug) =>
            new LoggerConfiguration().ConsoleOut(level).CreateLogger();

        /// <summary>
        /// Create trace logger
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="config"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static LoggerConfiguration Trace(this LoggerConfiguration configuration,
            IConfiguration config = null, LogEventLevel level = LogEventLevel.Debug) {
            if (config != null) {
                configuration = configuration.ReadFrom.Configuration(config);
            }
            return configuration
                .Enrich.WithProperty("SourceContext", null)
                .Enrich.FromLogContext()
                .WriteTo.Trace(outputTemplate: kDefaultTemplate)
                .MinimumLevel.Is(level);
        }

        /// <summary>
        /// Create trace logger
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public static ILogger Trace(LogEventLevel level = LogEventLevel.Debug) =>
            new LoggerConfiguration().Trace(null, level).CreateLogger();
    }
}
