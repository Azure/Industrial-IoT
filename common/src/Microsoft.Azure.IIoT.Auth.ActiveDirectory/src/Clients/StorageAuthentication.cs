// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Clients {
    using Microsoft.Azure.IIoT.Auth.Clients.Default;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Azure.IIoT.Auth;
    using System.Collections.Generic;
    using Autofac;
    using Serilog;

    /// <summary>
    /// Storage authentication support using managed service identity,
    /// service principal or local development (in order)
    /// </summary>
    public class StorageAuthentication : Module {

        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder) {

            builder.RegisterType<ClientAuthAggregateConfig>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<MsiStorageClientConfig>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<AadSpStorageConfig>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<MsiAuthenticationProvider>()
                .AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<AppAuthenticationProvider>()
                .AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<LocalDevelopmentProvider>()
                .AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<StorageTokenSource>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();

            base.Load(builder);
        }

        /// <summary>
        /// Authenticate with device token after trying app and developer authentication.
        /// </summary>
        internal class StorageTokenSource : TokenProviderAggregate, ITokenSource {
            /// <inheritdoc/>
            public StorageTokenSource(MsiAuthenticationProvider ma, AppAuthenticationProvider aa,
                LocalDevelopmentProvider ld, IEnumerable<ITokenProvider> providers, ILogger logger)
                    : base(providers, Http.Resource.Storage, logger, ma, aa, ld) {
            }
        }
    }
}
