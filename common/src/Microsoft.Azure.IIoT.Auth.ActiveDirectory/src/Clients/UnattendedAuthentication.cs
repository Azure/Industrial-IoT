// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Clients {
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Http.Auth;
    using Microsoft.Azure.IIoT.Utils;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Autofac;

    /// <summary>
    /// Unattended authentication support
    /// </summary>
    public class UnattendedAuthentication : Module {

        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder) {
            builder.RegisterModule<DefaultServiceAuthProviders>();

            builder.RegisterType<HttpBearerAuthentication>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ClientAuthAggregateConfig>()
                .AsImplementedInterfaces();

            // fallback to app authentication
            //   // Use auth service token provider
            //   builder.RegisterType<AuthServiceTokenProvider>()
            //       .AsSelf().AsImplementedInterfaces();
            builder.RegisterType<AppAuthenticationProvider>()
                .AsSelf().AsImplementedInterfaces();

            // Use service to service token source
            builder.RegisterType<ServiceTokenSource>()
                .AsImplementedInterfaces().SingleInstance();
            base.Load(builder);
        }

        /// <summary>
        /// First try passthrough, then try app authentication
        /// </summary>
        internal class ServiceTokenSource : ITokenSource {

            /// <inheritdoc/>
            public string Resource => Http.Resource.Platform;

            /// <inheritdoc/>
            public ServiceTokenSource(IComponentContext components) {
               // _as = components.Resolve<AuthServiceTokenProvider>();
                _aa = components.Resolve<AppAuthenticationProvider>();
            }

            /// <inheritdoc/>
            public async Task<TokenResultModel> GetTokenForAsync(
                IEnumerable<string> scopes = null) {

             //   var token = await Try.Async(() => _as.GetTokenForAsync(Resource, scopes));
             //   if (token != null) {
             //       return token;
             //   }
                return await Try.Async(() => _aa.GetTokenForAsync(Resource, scopes));
            }

            /// <inheritdoc/>
            public async Task InvalidateAsync() {
                //    await Try.Async(() =>_as.InvalidateAsync(Resource));
                await Try.Async(() => _aa.InvalidateAsync(Resource));
            }

            private readonly AppAuthenticationProvider _aa;
           // private readonly AuthServiceTokenProvider _as;
        }
    }
}
