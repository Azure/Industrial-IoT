// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Autofac {
    using Autofac.Core.Registration;
    using Furly.Extensions.Logging;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using System;

    /// <summary>
    /// Register console logger
    /// </summary>
    public static class ContainerBuilderEx {
        /// <summary>
        /// Register trace logger
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static IModuleRegistrar AddDiagnostics(this ContainerBuilder builder,
            Action<ILoggingBuilder> configure = null) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            configure ??= _ => { };
            builder.ConfigureServices(services => services.AddLogging(configure));
            builder.RegisterType<EmptyMetricsContext>()
                .AsImplementedInterfaces().IfNotRegistered(typeof(IMetricsContext));
            builder.RegisterType<HealthCheckRegistrar>()
                .AsImplementedInterfaces().SingleInstance();
            return builder.RegisterModule<LoggingModule>();
        }
    }
}
