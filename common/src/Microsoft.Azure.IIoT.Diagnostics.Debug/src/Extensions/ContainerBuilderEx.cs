// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Autofac {
    using Autofac.Core.Registration;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Serilog;
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
        public static IModuleRegistrar AddDiagnostics(this ContainerBuilder builder,
            IDiagnosticsConfig config = null, LoggerConfiguration log = null, bool addConsole = true) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            builder.RegisterType<EmptyMetricsContext>()
                .AsImplementedInterfaces().IfNotRegistered(typeof(IMetricsContext));
            builder.RegisterType<HealthCheckRegistrar>()
                .AsImplementedInterfaces().SingleInstance();
            return builder.RegisterModule(
                new LoggerProviderModule(new TraceLogger(log, addConsole)));
        }
    }
}
