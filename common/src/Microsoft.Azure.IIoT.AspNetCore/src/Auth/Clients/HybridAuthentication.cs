// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Auth.Clients {
    using Microsoft.Azure.IIoT.AspNetCore.Storage;
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Azure.IIoT.Auth.Clients.Default;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Http.Auth;
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.Azure.IIoT.Storage.Default;
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
                .AsImplementedInterfaces();
            builder.RegisterType<ClientAuthAggregateConfig>()
                .AsImplementedInterfaces();
            builder.RegisterType<DefaultTokenProvider>()
                .AsImplementedInterfaces()
                .IfNotRegistered(typeof(ITokenProvider));

            // Cache tokens in protected cache - by default in memory
            builder.RegisterType<MemoryCache>()
                .AsImplementedInterfaces().SingleInstance()
                .IfNotRegistered(typeof(ICache));
            builder.RegisterType<DistributedProtectedCache>()
                .AsImplementedInterfaces();

            // 1) Pass token through
            builder.RegisterType<PassThroughBearerToken>()
                .AsSelf().AsImplementedInterfaces();

            // 2) Fallback to client auth
            builder.RegisterType<HttpHandlerFactory>()
                .AsImplementedInterfaces();
            builder.RegisterType<ClientCredentialClient>()
                .AsSelf().AsImplementedInterfaces()
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);

            // 3) Use application auth provider
            builder.RegisterType<AppAuthenticationClient>()
                .AsSelf().AsImplementedInterfaces();

            // Use service to service token source
            builder.RegisterType<HybridTokenSource>()
                .AsImplementedInterfaces();
            base.Load(builder);
        }

        /// <summary>
        /// First try passthrough, then try service client credentials
        /// </summary>
        internal class HybridTokenSource : TokenClientAggregateSource, ITokenSource {
            /// <inheritdoc/>
            public HybridTokenSource(PassThroughBearerToken pt, ClientCredentialClient cc,
                AppAuthenticationClient aa, IEnumerable<ITokenClient> providers, ILogger logger)
                    : base(providers, Http.Resource.Platform, logger, pt, cc, aa) {
            }
        }
    }
}
