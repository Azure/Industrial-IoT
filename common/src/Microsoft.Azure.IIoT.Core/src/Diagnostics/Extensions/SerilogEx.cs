// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Serilog {
    using Microsoft.Azure.IIoT;
    using Microsoft.Azure.IIoT.Diagnostics;
    using System;
    using System.Diagnostics;
    using System.IO;

    /// <summary>
    /// Serilog extensions
    /// </summary>
    public static class SerilogEx {

        /// <summary>
        /// Configure logging
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="configure"></param>
        /// <param name="addConsole"></param>
        /// <returns></returns>
        public static LoggerConfiguration Configure(this LoggerConfiguration configuration,
            Func<LoggerConfiguration, string, LoggerConfiguration> configure, bool addConsole = true) {
            configuration = configuration
                .Enrich.WithProperty("SourceContext", "Root")
                .Enrich.FromLogContext();
            if (addConsole) {
                configuration = configuration.WriteTo.Console(outputTemplate: kDefaultTemplate);
            }

            var path = Environment.GetEnvironmentVariable(PcsVariable.PCS_LOGS_PATH);
            if (!string.IsNullOrEmpty(path)) {
                configuration = configuration.WriteTo.File(Path.Combine(path,
                    Process.GetCurrentProcess().ProcessName +
                    Process.GetCurrentProcess().Id + ".log"),
                    flushToDiskInterval: TimeSpan.FromSeconds(1),
                    outputTemplate: kDefaultTemplate);
            }

            configuration = configure(configuration, kDefaultTemplate);
            return configuration.MinimumLevel.ControlledBy(LogControl.Level);
        }

        /// <summary>
        /// Create console logger
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static LoggerConfiguration Console(this LoggerConfiguration configuration) {
            return configuration.Configure((c, m) => c, true);
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
                .MinimumLevel.ControlledBy(LogControl.Level);
        }

        private const string kDefaultTemplate =
            "[{Timestamp:u} {Level:u3} {SourceContext:lj}] {Message:lj} {NewLine}{Exception}";
    }
}
