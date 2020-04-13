// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Clients.Default {
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Azure.IIoT.Http.Auth;
    using Microsoft.Azure.IIoT.Utils;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Autofac;

    /// <summary>
    /// Cli authentication
    /// </summary>
    public class NativeClientAuthentication : Module {

        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder) {

            builder.RegisterType<HttpBearerAuthentication>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ClientAuthAggregateConfig>()
                .AsImplementedInterfaces();

            // Use Vs authentication
            builder.RegisterType<LocalDevelopmentProvider>()
                .AsSelf();
            // Use app authentication
            builder.RegisterType<AppAuthenticationProvider>()
                .AsSelf();
            // fallback to device code token provider
            builder.RegisterType<DeviceCodeTokenProvider>()
                .AsSelf();

            // Use cli token source
            builder.RegisterType<NativeClientTokenSource>()
                .AsImplementedInterfaces().SingleInstance();
            base.Load(builder);
        }


        /// <summary>
        /// Authenticate with device token after trying app authentication.
        /// </summary>
        internal class NativeClientTokenSource : ITokenSource {

            /// <inheritdoc/>
            public string Resource => Http.Resource.Platform;

            /// <inheritdoc/>
            public NativeClientTokenSource(IComponentContext components) {
                _vs = components.Resolve<LocalDevelopmentProvider>();
                _dc = components.Resolve<DeviceCodeTokenProvider>();
                _aa = components.Resolve<AppAuthenticationProvider>();
            }

            /// <inheritdoc/>
            public async Task<TokenResultModel> GetTokenForAsync(
                IEnumerable<string> scopes = null) {

                var token = await Try.Async(() => _vs.GetTokenForAsync(Resource, scopes));
                if (token != null) {
                    return token;
                }
                token = await Try.Async(() => _aa.GetTokenForAsync(Resource, scopes));
                if (token != null) {
                    return token;
                }
                return await Try.Async(() => _dc.GetTokenForAsync(Resource, scopes));
            }

            /// <inheritdoc/>
            public async Task InvalidateAsync() {
                await _aa.InvalidateAsync(Resource);
                await _vs.InvalidateAsync(Resource);
                await _dc.InvalidateAsync(Resource);
            }

            private readonly LocalDevelopmentProvider _vs;
            private readonly DeviceCodeTokenProvider _dc;
            private readonly AppAuthenticationProvider _aa;
        }
    }
}
