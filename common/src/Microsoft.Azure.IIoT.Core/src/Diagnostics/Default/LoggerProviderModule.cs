// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using Serilog;
    using System;
    using System.Linq;
    using Autofac;
    using Autofac.Core;
    using Autofac.Core.Activators.Reflection;
    using Autofac.Core.Registration;
    using Module = Autofac.Module;

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
            if (singleton) {
                if (_instance == null) {
                    lock (kSingleton) {
                        if (_instance == null) {
                            _instance = provider;
                        }
                    }
                }
                _provider = _instance;
            }
            else {
                _provider = provider;
            }
        }

        /// <summary>
        /// Logger provider modules with pass through logger (static logger)
        /// </summary>
        public LoggerProviderModule() :
            this (new DefaultProvider(), false) {
        }

        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder) {
            builder.Register((c, p) => {
                var targetType = p.OfType<NamedParameter>()
                    .FirstOrDefault(np =>
                        np.Name == kTargetTypeParameterName && np.Value is Type);
                if (targetType != null) {
                    return _provider.Logger.ForContext((Type)targetType.Value);
                }
                return _provider.Logger;
            }).As<ILogger>().ExternallyOwned();
        }

        /// <inheritdoc/>
        protected override void AttachToComponentRegistration(IComponentRegistryBuilder registry,
            IComponentRegistration registration) {
            // Ignore components that provide loggers (and thus avoid a circular dependency below)
            if (registration.Services
                .OfType<TypedService>()
                .Any(ts => ts.ServiceType == typeof(ILogger) ||
                           ts.ServiceType == typeof(ILoggerProvider))) {
                return;
            }
            if (registration.Activator is ReflectionActivator ra) {
                try {
                    var ctors = ra.ConstructorFinder.FindConstructors(ra.LimitType);
                    var usesLogger = ctors
                        .SelectMany(ctor => ctor.GetParameters())
                        .Any(pi => pi.ParameterType == typeof(ILogger));
                    // Ignore components known to be without logger dependencies
                    if (!usesLogger) {
                        return;
                    }
                }
                catch (NoConstructorsFoundException) {
                    return; // No need
                }
            }
            registration.Preparing += (sender, args) => {
                var log = args.Context.Resolve<ILogger>().ForContext(registration.Activator.LimitType);
                args.Parameters = new[] { TypedParameter.From(log) }.Concat(args.Parameters);
            };
        }

        private class DefaultProvider : ILoggerProvider {

            /// <inheritdoc/>
            public ILogger Logger { get; } = Log.Logger ??
                new LoggerConfiguration().Console().CreateLogger();
        }

        private const string kTargetTypeParameterName = "Autofac.AutowiringPropertyInjector.InstanceType";
        private static readonly object kSingleton = new object();
        private static ILoggerProvider _instance;
        private readonly ILoggerProvider _provider;
    }
}
