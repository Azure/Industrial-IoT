// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Runtime {
    using Autofac;

    /// <summary>
    /// Register default authentication providers for public clients
    /// </summary>
    public class DefaultPublicClientAuthProviders : Module {

        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder) {

            builder.RegisterType<ClientAuthAggregateConfig>()
                .AsImplementedInterfaces();

            builder.RegisterType<AadApiClientConfig>()
                .AsImplementedInterfaces();

            // ...

            builder.RegisterType<AuthServiceApiClientConfig>()
                .AsImplementedInterfaces();

            base.Load(builder);
        }
    }
}
