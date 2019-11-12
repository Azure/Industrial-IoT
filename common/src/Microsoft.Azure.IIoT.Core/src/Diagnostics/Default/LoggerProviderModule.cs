// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using Autofac;
    using AutofacSerilogIntegration;
    using System;

    /// <summary>
    /// Logger provider module
    /// </summary>
    public class LoggerProviderModule : Module {

        /// <summary>
        /// Create module
        /// </summary>
        /// <param name="provider"></param>
        public LoggerProviderModule(ILoggerProvider provider) {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// Override
        /// </summary>
        /// <param name="builder"></param>
        protected override void Load(ContainerBuilder builder) {
            builder.RegisterLogger(_provider.Logger);
            base.Load(builder);
        }

        private readonly ILoggerProvider _provider;
    }
}
