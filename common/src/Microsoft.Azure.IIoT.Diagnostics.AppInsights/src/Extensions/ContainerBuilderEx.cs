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
    /// Register loggers
    /// </summary>
    public static class AppInsightsContainerBuilderEx {

        /// <summary>
        /// Register telemetry client diagnostics logger
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="config"></param>
        /// <param name="log"></param>
        /// <param name="addConsole"></param>
        /// <returns></returns>
        public static IModuleRegistrar AddDiagnostics(this ContainerBuilder builder,
            IDiagnosticsConfig config, LoggerConfiguration log = null, bool addConsole = true) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            if (config == null) {
                throw new ArgumentNullException(nameof(config));
            }
            // Register metrics logger
            builder.RegisterType<ApplicationInsightsMetrics>()
                .AsImplementedInterfaces();
            builder.RegisterType<HealthCheckRegistrar>()
                .AsImplementedInterfaces().SingleInstance();
            return builder.RegisterModule(
                new LoggerProviderModule(new ApplicationInsightsLogger(config, log, addConsole)));
        }
    }
}
