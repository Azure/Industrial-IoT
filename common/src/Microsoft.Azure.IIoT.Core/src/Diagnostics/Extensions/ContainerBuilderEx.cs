// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Autofac {
    using Serilog;
    using Autofac.Core.Registration;
    using Microsoft.Azure.IIoT.Diagnostics;
    using System;

    /// <summary>
    /// Register console logger
    /// </summary>
    public static class ContainerBuilderEx {

        /// <summary>
        /// Register console logger
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IModuleRegistrar AddConsoleLogger(this ContainerBuilder builder,
            LoggerConfiguration configuration = null) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            return builder.RegisterModule(
                new LoggerProviderModule(new ConsoleLogger(configuration)));
        }
    }
}
