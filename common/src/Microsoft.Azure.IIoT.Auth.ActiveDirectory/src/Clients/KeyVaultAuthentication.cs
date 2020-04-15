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
    /// Keyvault authentication support using managed service identity,
    /// service principal configuration or local development (in order)
    /// </summary>
    public class KeyVaultAuthentication : Module {

        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder) {

            builder.RegisterType<ClientAuthAggregateConfig>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<MsiKeyVaultClientConfig>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<AadSpKeyVaultConfig>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<MsiAuthenticationProvider>()
                .AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<AppAuthenticationProvider>()
                .AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<LocalDevelopmentProvider>()
                .AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<KeyVaultTokenSource>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();

            base.Load(builder);
        }

        /// <summary>
        /// Authenticate with device token after trying app and developer authentication.
        /// </summary>
        internal class KeyVaultTokenSource : TokenProviderAggregate, ITokenSource {
            /// <inheritdoc/>
            public KeyVaultTokenSource(MsiAuthenticationProvider ma, AppAuthenticationProvider aa,
                LocalDevelopmentProvider ld, IEnumerable<ITokenProvider> providers, ILogger logger)
                    : base(providers, Http.Resource.KeyVault, logger, ma, aa, ld) {
            }
        }
    }
}
