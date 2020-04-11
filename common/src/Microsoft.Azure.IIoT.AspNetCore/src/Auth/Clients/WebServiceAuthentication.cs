// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Auth.Clients {
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Azure.IIoT.Http.Auth;
    using Autofac;

    /// <summary>
    /// Default web service authentication
    /// </summary>
    public class WebServiceAuthentication : Module {

        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder) {
            builder.RegisterModule<DefaultServiceAuthProviders>();

            builder.RegisterType<HttpBearerAuthentication>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ClientAuthAggregateConfig>()
                .AsImplementedInterfaces();
            builder.RegisterType<DistributedTokenCache>()
                .AsImplementedInterfaces().SingleInstance();

            // Pass token through is the only provider here
            builder.RegisterType<PassThroughTokenProvider>()
                .AsSelf().AsImplementedInterfaces();
            builder.RegisterType<TokenProviderTokenSource<PassThroughTokenProvider>>()
                .AsImplementedInterfaces().SingleInstance();
            base.Load(builder);
        }
    }
}
