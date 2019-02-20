// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Autofac;
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
        public ILogger Logger { get; }

        /// <summary>
        /// Create stack logger
        /// </summary>
        /// <param name="logger"></param>
        public StackLogger(ILogger logger) {
            Logger = logger;
        }

        /// <inheritdoc/>
        public void Start() {

            // Disable traditional logging
            Utils.SetTraceMask(0);
            Utils.SetTraceOutput(Utils.TraceOutput.Off);

            // Register callback
            Utils.Tracing.TraceEventHandler += Tracing_TraceEventHandler;
        }

        /// <inheritdoc/>
        public void Dispose() {

            // Unregister callback
            Utils.Tracing.TraceEventHandler -= Tracing_TraceEventHandler;
        }

        /// <summary>
        /// Helper to use when not using autofac di.
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static StackLogger Create(ILogger logger) {
            var stackLogger = new StackLogger(logger);
            stackLogger.Start();
            return stackLogger;
        }

        /// <summary>
        /// Log to logger
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Tracing_TraceEventHandler(object sender, TraceEventArgs e) {
            if (!string.IsNullOrEmpty(e.Format)) {
                var traceName = ToLogLevel(e.TraceMask, out var level);
                Logger.Write(level, e.Exception, traceName + ":" + e.Format,
                    e.Arguments);
            }
        }

        /// <summary>
        /// Convert to loglevel and trace name string
        /// </summary>
        /// <param name="traceMask"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        private static string ToLogLevel(int traceMask, out LogEventLevel level) {
            switch (traceMask) {
                case Utils.TraceMasks.Error:
                    level = LogEventLevel.Error;
                    return nameof(Utils.TraceMasks.Error);
                case Utils.TraceMasks.Information:
                    // level = LogLevel.Info; // TOO VERBOSE
                    level = LogEventLevel.Verbose;
                    return nameof(Utils.TraceMasks.Information);
                case Utils.TraceMasks.StartStop:
                    level = LogEventLevel.Information;
                    return nameof(Utils.TraceMasks.StartStop);
                case Utils.TraceMasks.Operation:
                    level = LogEventLevel.Debug;
                    return nameof(Utils.TraceMasks.Operation);
                case Utils.TraceMasks.ExternalSystem:
                    level = LogEventLevel.Debug;
                    return nameof(Utils.TraceMasks.ExternalSystem);
                case Utils.TraceMasks.StackTrace:
                    level = LogEventLevel.Verbose;
                    return nameof(Utils.TraceMasks.Service);
                case Utils.TraceMasks.Service:
                    level = LogEventLevel.Verbose;
                    return nameof(Utils.TraceMasks.Service);
                case Utils.TraceMasks.ServiceDetail:
                    level = LogEventLevel.Verbose;
                    return nameof(Utils.TraceMasks.ServiceDetail);
                case Utils.TraceMasks.OperationDetail:
                    level = LogEventLevel.Verbose;
                    return nameof(Utils.TraceMasks.OperationDetail);
                case Utils.TraceMasks.Security:
                    level = LogEventLevel.Verbose;
                    return nameof(Utils.TraceMasks.Security);
                default:
                    level = LogEventLevel.Verbose;
                    return "unknown";
            }
        }
    }
}
