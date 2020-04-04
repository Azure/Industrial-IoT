// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Autofac {
    using Autofac.Core.Registration;
    using Serilog;
    using Microsoft.Azure.IIoT.Diagnostics;
    using System;

    /// <summary>
    /// Register trace logger
    /// </summary>
    public static class DebugContainerBuilderEx {

        /// <summary>
        /// Register trace logger
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="config"></param>
        /// <param name="log"></param>
        /// <param name="addConsole"></param>
        /// <returns></returns>
#pragma warning disable IDE0060 // Remove unused parameter
        public static IModuleRegistrar AddDiagnostics(this ContainerBuilder builder,
            IDiagnosticsConfig config = null, LoggerConfiguration log = null, bool addConsole = true) {
#pragma warning restore IDE0060 // Remove unused parameter
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            // Register metrics logger
            builder.RegisterType<MetricsLogger>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<HealthCheckRegistrar>()
                .AsImplementedInterfaces().SingleInstance();
            return builder.RegisterModule(
                new LoggerProviderModule(new TraceLogger(log, addConsole)));
        }
    }
}
