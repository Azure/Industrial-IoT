// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Clients {
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Azure.IIoT.Auth.Clients.Default;
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Http.Auth;
    using Microsoft.Azure.IIoT.Storage.Default;
    using Autofac;
    using Serilog;
    using System.Collections.Generic;

    /// <summary>
    /// Unattended client to service authentication support
    /// </summary>
    public class UnattendedAuthentication : Module {

        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder) {
            builder.RegisterModule<DefaultServiceAuthProviders>();

            builder.RegisterType<HttpBearerAuthentication>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<ClientAuthAggregateConfig>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();

            // Use client credential and fallback to app authentication
            builder.RegisterType<HttpHandlerFactory>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<ClientCredentialClient>()
                .AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope()
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);
            builder.RegisterType<AppAuthenticationClient>()
                .AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();

            // Use service to service token source
            builder.RegisterType<ServiceTokenSource>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<MemoryCache>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<CachingTokenProvider>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            base.Load(builder);
        }

        /// <summary>
        /// Service token strategy prefers client credentials over app auth and rest
        /// </summary>
        internal class ServiceTokenSource : TokenClientAggregateSource, ITokenSource {
            /// <inheritdoc/>
            public ServiceTokenSource(ClientCredentialClient cc, AppAuthenticationClient aa,
                IEnumerable<ITokenClient> providers, ILogger logger)
                    : base(providers, Http.Resource.Platform, logger, cc, aa) {
            }
        }
    }
}
