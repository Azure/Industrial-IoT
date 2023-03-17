// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Autofac
{
    using Furly.Extensions.Logging;
    using Microsoft.Azure.IIoT.Diagnostics;

    /// <summary>
    /// Register console logger
    /// </summary>
    public static class ContainerBuilderEx
    {
        /// <summary>
        /// Register diagnostics
        /// </summary>
        /// <param name="builder"></param>
        public static ContainerBuilder AddDiagnostics(this ContainerBuilder builder)
        {
            builder.RegisterInstance(IMetricsContext.Empty)
                .AsImplementedInterfaces().IfNotRegistered(typeof(IMetricsContext));
            builder.RegisterType<HealthCheckRegistrar>()
                .AsImplementedInterfaces().SingleInstance();
            return builder;
        }
    }
}
