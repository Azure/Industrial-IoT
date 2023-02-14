// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Auth.Runtime {
    using Autofac;
    using Microsoft.Azure.IIoT.Api.Runtime;
    using Microsoft.Azure.IIoT.Auth.Runtime;

    /// <summary>
    /// Register default authentication providers for confidential clients
    /// </summary>
    public class DefaultConfidentialClientAuthProviders : Module {

        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder) {

            builder.RegisterType<ClientAuthAggregateConfig>()
                .AsImplementedInterfaces();

            builder.RegisterType<AadApiWebConfig>()
                .AsImplementedInterfaces();

            // ...

            builder.RegisterType<AuthServiceApiWebConfig>()
                .AsImplementedInterfaces();

            base.Load(builder);
        }
    }
}
