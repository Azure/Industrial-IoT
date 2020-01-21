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
        /// Create module - if provider should regarded as singleton use
        /// singleton instance.   This is useful for situations where an
        /// outer container has a root logger.
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="singleton"></param>
        public LoggerProviderModule(ILoggerProvider provider, bool singleton = true) {
            if (provider == null) {
                throw new ArgumentNullException(nameof(provider));
            }
            if (_instance == null) {
                lock (kSingleton) {
                    if (_instance == null) {
                        _instance = provider;
                    }
                }
            }
            if (singleton) {
                _provider = _instance;
            }
        }

        /// <summary>
        /// Override
        /// </summary>
        /// <param name="builder"></param>
        protected override void Load(ContainerBuilder builder) {
            builder.RegisterLogger(_provider.Logger);
            base.Load(builder);
        }

        private static readonly object kSingleton = new object();
        private static ILoggerProvider _instance;
        private readonly ILoggerProvider _provider;
    }
}
