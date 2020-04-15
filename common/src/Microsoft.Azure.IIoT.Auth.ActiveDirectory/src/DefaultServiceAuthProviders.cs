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
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ClientAuthAggregateConfig>()
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<AadServiceAuthConfig>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<AadSpClientConfig>()
                .AsImplementedInterfaces().SingleInstance();

            // ...

            builder.RegisterType<MsiKeyVaultClientConfig>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<AuthServiceOAuthConfig>()
                .AsImplementedInterfaces().SingleInstance();

            base.Load(builder);
        }
    }
}
