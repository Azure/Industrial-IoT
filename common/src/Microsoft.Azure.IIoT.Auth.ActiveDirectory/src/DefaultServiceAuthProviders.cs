// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Runtime {
    using Autofac;

    /// <summary>
    /// Register default authentication providers
    /// </summary>
    public class DefaultServiceAuthProviders : Module {

        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder) {

            builder.RegisterType<ServiceAuthAggregateConfig>()
                .AsImplementedInterfaces();
            builder.RegisterType<ClientAuthAggregateConfig>()
                .AsImplementedInterfaces();

            builder.RegisterType<AadServiceAuthConfig>()
                .AsImplementedInterfaces();
            builder.RegisterType<AadSpClientConfig>()
                .AsImplementedInterfaces();

            // ...

            builder.RegisterType<MsiKeyVaultClientConfig>()
                .AsImplementedInterfaces();
            builder.RegisterType<AuthServiceOAuthConfig>()
                .AsImplementedInterfaces();

            base.Load(builder);
        }
    }
}
