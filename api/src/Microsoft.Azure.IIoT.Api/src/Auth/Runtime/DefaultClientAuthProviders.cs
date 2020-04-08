// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Runtime {
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Autofac;

    /// <summary>
    /// Register default authentication providers
    /// </summary>
    public class DefaultClientAuthProviders : Module {

        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder) {

            builder.RegisterType<ClientAuthAggregateConfig>()
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<AadApiClientConfig>()
                .AsImplementedInterfaces().SingleInstance();

            // ...

            builder.RegisterType<AuthServiceApiClientConfig>()
                .AsImplementedInterfaces().SingleInstance();

            base.Load(builder);
        }
    }

}
