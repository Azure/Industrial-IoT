// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Auth.Clients {
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Azure.IIoT.Http.Auth;
    using Autofac;
    using Microsoft.Azure.IIoT.Auth;

    /// <summary>
    /// Default web service authentication
    /// </summary>
    public class WebApiAuthentication : Module {

        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder) {

            builder.RegisterType<HttpBearerAuthentication>()
                .AsImplementedInterfaces();
            builder.RegisterType<ClientAuthAggregateConfig>()
                .AsImplementedInterfaces();
            builder.RegisterType<DefaultTokenProvider>()
                .AsImplementedInterfaces()
                .IfNotRegistered(typeof(ITokenProvider));

            builder.RegisterModule<DefaultServiceAuthProviders>();

            // Pass token through is the only provider here
            builder.RegisterType<PassThroughBearerToken>()
                .AsSelf();
            builder.RegisterType<TokenClientSource<PassThroughBearerToken>>()
                .AsImplementedInterfaces();

            base.Load(builder);
        }
    }
}
