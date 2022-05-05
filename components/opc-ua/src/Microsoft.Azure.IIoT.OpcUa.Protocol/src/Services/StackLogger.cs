// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Autofac;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Serilog;
    using Serilog.Events;
    using System;

    /// <summary>
    /// Injectable service that registers logger with stack
    /// </summary>
    public class StackLogger : IStartable, IDisposable {

        /// <summary>
        /// Wrapped logger
        /// </summary>
        public Serilog.ILogger Logger { get; }

        /// <summary>
        /// Create stack logger
        /// </summary>
        /// <param name="logger"></param>
        public StackLogger(Serilog.ILogger logger) {
            Logger = logger;
        }

        /// <inheritdoc/>
        public void Start() {
            var opcStackTraceMask = Utils.TraceMasks.Error | Utils.TraceMasks.Security;
            var minimumLogLevel = LogLevel.Trace;
            switch (LogControl.Level.MinimumLevel) {
                case LogEventLevel.Fatal:
                    minimumLogLevel = LogLevel.Critical;
                    opcStackTraceMask |= 0;
                    break;
                case LogEventLevel.Error:
                    minimumLogLevel = LogLevel.Error;
                    opcStackTraceMask |= Utils.TraceMasks.StackTrace;
                    break;
                case LogEventLevel.Warning:
                    minimumLogLevel = LogLevel.Warning;
                    opcStackTraceMask |= Utils.TraceMasks.StackTrace;
                    break;
                case LogEventLevel.Information:
                    minimumLogLevel = LogLevel.Information;
                    opcStackTraceMask |= Utils.TraceMasks.StartStop | Utils.TraceMasks.StackTrace | Utils.TraceMasks.Information;
                    break;
                case LogEventLevel.Debug:
                    minimumLogLevel = LogLevel.Debug;
                    opcStackTraceMask |= Utils.TraceMasks.All;
                    break;
                case LogEventLevel.Verbose:
                    minimumLogLevel = LogLevel.Trace;
                    opcStackTraceMask |= Utils.TraceMasks.All;
                    break;
            }

            var logger = LoggerFactory
                .Create(builder => builder.SetMinimumLevel(minimumLogLevel))
                .AddSerilog(Logger)
                .CreateLogger("OpcUa");
            Utils.SetLogger(logger);
            Utils.SetTraceMask(opcStackTraceMask);
        }

        /// <inheritdoc/>
        public void Dispose() {

        }

        /// <summary>
        /// Helper to use when not using autofac di.
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static StackLogger Create(Serilog.ILogger logger) {
            var stackLogger = new StackLogger(logger);
            stackLogger.Start();
            return stackLogger;
        }
    }
}
