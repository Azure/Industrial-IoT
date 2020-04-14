// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Auth.Clients {
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Azure.IIoT.Auth.Clients.Default;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Http.Auth;
    using Serilog;
    using System.Collections.Generic;
    using Autofac;

    /// <summary>
    /// Hybrid web service and unattended authentication
    /// </summary>
    public class HybridAuthentication : Module {

        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder) {
            builder.RegisterModule<DefaultServiceAuthProviders>();

            builder.RegisterType<HttpBearerAuthentication>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<ClientAuthAggregateConfig>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<DistributedTokenCache>()
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<PassThroughTokenProvider>()
                .AsSelf().AsImplementedInterfaces();
            builder.RegisterType<HttpHandlerFactory>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<ClientCredentialProvider>()
                .AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope()
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);
            builder.RegisterType<AppAuthenticationProvider>()
                .AsSelf().AsImplementedInterfaces();

            // Use service to service token source
            builder.RegisterType<HybridTokenSource>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            base.Load(builder);
        }

        /// <summary>
        /// First try passthrough, then try service client credentials
        /// </summary>
        internal class HybridTokenSource : TokenProviderAggregate, ITokenSource {
            /// <inheritdoc/>
            public HybridTokenSource(PassThroughTokenProvider pt, ClientCredentialProvider cc,
                AppAuthenticationProvider aa, IEnumerable<ITokenProvider> providers, ILogger logger)
                    : base(providers, Http.Resource.Platform, logger, pt, cc, aa) {
            }
        }
    }
}
