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
    using Autofac;
    using Serilog;
    using System.Collections.Generic;

    /// <summary>
    /// Unattended client authentication support
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
            builder.RegisterType<ClientCredentialProvider>()
                .AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope()
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);
            builder.RegisterType<AppAuthenticationProvider>()
                .AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();

            // Use service to service token source
            builder.RegisterType<ServiceTokenSource>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            base.Load(builder);
        }

        /// <summary>
        /// Service token strategy prefers client credentials over app auth and rest
        /// </summary>
        internal class ServiceTokenSource : TokenProviderAggregate, ITokenSource {
            /// <inheritdoc/>
            public ServiceTokenSource(ClientCredentialProvider cc, AppAuthenticationProvider aa,
                IEnumerable<ITokenProvider> providers, ILogger logger)
                    : base(providers, Http.Resource.Platform, logger, cc, aa) {
            }
        }
    }
}
