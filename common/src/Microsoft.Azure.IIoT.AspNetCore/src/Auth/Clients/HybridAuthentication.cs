// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Auth.Clients {
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Http.Auth;
    using Microsoft.Azure.IIoT.Utils;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Autofac;
    using Microsoft.Azure.IIoT.Auth.Clients.Default;

    /// <summary>
    /// Hybrid web service and unattended authentication
    /// </summary>
    public class HybridAuthentication : Module {

        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder) {
            builder.RegisterModule<DefaultServiceAuthProviders>();

            builder.RegisterType<HttpBearerAuthentication>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ClientAuthAggregateConfig>()
                .AsImplementedInterfaces();

            builder.RegisterType<DistributedTokenCache>()
                .AsImplementedInterfaces().SingleInstance();

            // Pass token through
            builder.RegisterType<PassThroughTokenProvider>()
                .AsSelf().AsImplementedInterfaces();
            // Use auth service token provider
            builder.RegisterType<ClientCredentialTokenProvider>()
                .AsSelf().AsImplementedInterfaces();
            // fallback to app authentication
            builder.RegisterType<AppAuthenticationProvider>()
                .AsSelf().AsImplementedInterfaces();

            // Use service to service token source
            builder.RegisterType<HybridTokenSource>()
                .AsImplementedInterfaces().SingleInstance();
            base.Load(builder);
        }

        /// <summary>
        /// First try passthrough, then try app authentication
        /// </summary>
        internal class HybridTokenSource : ITokenSource {

            /// <inheritdoc/>
            public string Resource => Http.Resource.Platform;

            /// <inheritdoc/>
            public HybridTokenSource(IComponentContext components) {
                _pt = components.Resolve<PassThroughTokenProvider>();
                _cc = components.Resolve<ClientCredentialTokenProvider>();
                _aa = components.Resolve<AppAuthenticationProvider>();
            }

            /// <inheritdoc/>
            public async Task<TokenResultModel> GetTokenForAsync(
                IEnumerable<string> scopes = null) {

                var token = await Try.Async(() => _pt.GetTokenForAsync(Resource, scopes));
                if (token != null) {
                    return token;
                }
                token = await Try.Async(() => _cc.GetTokenForAsync(Resource, scopes));
                if (token != null) {
                    return token;
                }
                return await Try.Async(() => _aa.GetTokenForAsync(Resource, scopes));
            }

            /// <inheritdoc/>
            public async Task InvalidateAsync() {
                await Try.Async(() => _pt.InvalidateAsync(Resource));
                await Try.Async(() =>_cc.InvalidateAsync(Resource));
                await Try.Async(() => _aa.InvalidateAsync(Resource));
            }

            private readonly PassThroughTokenProvider _pt;
            private readonly AppAuthenticationProvider _aa;
            private readonly ClientCredentialTokenProvider _cc;
        }
    }
}
