// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Serilog {
    using Serilog.Events;
    using Serilog.Core;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Azure.IIoT.Diagnostics;

    /// <summary>
    /// Serilog extensions
    /// </summary>
    public static class LogEx {

        /// <summary>
        /// Level switcher
        /// </summary>
        public static LoggingLevelSwitch Level { get; } = new LoggingLevelSwitch();

        /// <summary>
        /// Create console logger
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static LoggerConfiguration Console(this LoggerConfiguration configuration,
            IConfiguration config = null) {
            if (config != null) {
                configuration = configuration.ReadFrom.Configuration(config);
            }
            return configuration
                .Enrich.WithProperty("SourceContext", null)
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: kDefaultTemplate)
                .MinimumLevel.ControlledBy(Level);
        }

        /// <summary>
        /// Create standard console logger
        /// </summary>
        /// <returns></returns>
        public static ILogger Console() {
            var level = LogEventLevel.Information;
#if DEBUG
            level = LogEventLevel.Debug;
#endif
            return Console(level);
        }

        /// <summary>
        /// Create standard console logger
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public static ILogger Console(LogEventLevel level) {
            Level.MinimumLevel = level;
            return new LoggerConfiguration().Console().CreateLogger();
        }

        /// <summary>
        /// Create standard console logger
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static ILogger Console(IConfiguration config = null) {
            if (config != null) {
                Level.MinimumLevel = config.GetValue("LogLevel", LogEventLevel.Information);
            }
            return new LoggerConfiguration().Console(config).CreateLogger();
        }

        /// <summary>
        /// Create rolling file logger
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="path"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static LoggerConfiguration RollingFile(this LoggerConfiguration configuration,
            string path, IConfiguration config = null) {
            if (config != null) {
                configuration = configuration.ReadFrom.Configuration(config);
            }
            return configuration
                .Enrich.WithProperty("SourceContext", null)
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: kDefaultTemplate)
                .WriteTo.File(path, outputTemplate: kDefaultTemplate, rollingInterval: RollingInterval.Day)
                .MinimumLevel.ControlledBy(Level);
        }

        /// <summary>
        /// Create rolling file logger
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static ILogger RollingFile(string path) {
            var level = LogEventLevel.Information;
#if DEBUG
            level = LogEventLevel.Debug;
#endif
            return RollingFile(path, level);
        }

        /// <summary>
        /// Create rolling file logger
        /// </summary>
        /// <param name="path"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static ILogger RollingFile(string path, LogEventLevel level) {
            Level.MinimumLevel = level;
            return new LoggerConfiguration().RollingFile(path).CreateLogger();
        }

        /// <summary>
        /// Create rolling file logger
        /// </summary>
        /// <param name="path"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static ILogger RollingFile(string path, IConfiguration config = null) {
            if (config != null) {
                Level.MinimumLevel = config.GetValue("LogLevel", LogEventLevel.Information);
            }
            return new LoggerConfiguration().RollingFile(path, config).CreateLogger();
        }

        /// <summary>
        /// Create simple console out like logger
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static LoggerConfiguration ConsoleOut(this LoggerConfiguration configuration) {
            return configuration
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: "{Message:lj}{NewLine}")
                .MinimumLevel.ControlledBy(Level);
        }

        /// <summary>
        /// Create console out like logger
        /// </summary>
        /// <returns></returns>
        public static ILogger ConsoleOut() {
            var level = LogEventLevel.Information;
#if DEBUG
            level = LogEventLevel.Debug;
#endif
            return ConsoleOut(level);
        }

        /// <summary>
        /// Create console out like logger
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public static ILogger ConsoleOut(LogEventLevel level) {
            Level.MinimumLevel = level;
            return new LoggerConfiguration().ConsoleOut().CreateLogger();
        }

        /// <summary>
        /// Create application insights logger
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="aiConfig"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static LoggerConfiguration ApplicationInsights(
            this LoggerConfiguration configuration,
            IApplicationInsightsConfig aiConfig = null, IConfiguration config = null) {
            if (config != null) {
                configuration = configuration.ReadFrom.Configuration(config);
            }
            return configuration
                .Enrich.WithProperty("SourceContext", null)
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: kDefaultTemplate)
                .WriteTo.ApplicationInsights(aiConfig?.TelemetryConfiguration ?? null, TelemetryConverter.Traces)
                .MinimumLevel.ControlledBy(Level);
        }

        /// <summary>
        /// Create application insights logger
        /// </summary>
        /// <param name="aiConfig"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static ILogger ApplicationInsights(IApplicationInsightsConfig aiConfig,
            IConfiguration config = null) {
            var level = LogEventLevel.Information;
#if DEBUG
            level = LogEventLevel.Debug;
#endif
            return ApplicationInsights(aiConfig, config, level);
        }

        /// <summary>
        /// Create application insights logger
        /// </summary>
        /// <param name="aiConfig"></param>
        /// <param name="config"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static ILogger ApplicationInsights(IApplicationInsightsConfig aiConfig,
            IConfiguration config, LogEventLevel level) {
            Level.MinimumLevel = level;
            return new LoggerConfiguration().ApplicationInsights(aiConfig, config).CreateLogger();
        }

        /// <summary>
        /// Create trace logger
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static LoggerConfiguration Trace(this LoggerConfiguration configuration,
            IConfiguration config = null) {
            if (config != null) {
                configuration = configuration.ReadFrom.Configuration(config);
            }
            return configuration
                .Enrich.WithProperty("SourceContext", null)
                .Enrich.FromLogContext()
                .WriteTo.Trace(outputTemplate: kDefaultTemplate)
                .MinimumLevel.ControlledBy(Level);
        }

        /// <summary>
        /// Create trace logger
        /// </summary>
        /// <returns></returns>
        public static ILogger Trace() {
            var level = LogEventLevel.Information;
#if DEBUG
            level = LogEventLevel.Debug;
#endif
            return Trace(level);
        }

        /// <summary>
        /// Create trace logger
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public static ILogger Trace(LogEventLevel level) {
            Level.MinimumLevel = level;
            return new LoggerConfiguration().Trace().CreateLogger();
        }

        /// <summary>
        /// Create trace logger
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static ILogger Trace(IConfiguration config = null) {
            if (config != null) {
                Level.MinimumLevel = config.GetValue("LogLevel", LogEventLevel.Information);
            }
            return new LoggerConfiguration().Trace(config).CreateLogger();
        }

        private const string kDefaultTemplate =
            "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} ({SourceContext:l}){NewLine}{Exception}";
    }
}
