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
    public class ConsoleAuthentication : Module {

        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder) {

            builder.RegisterType<HttpBearerAuthentication>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ClientAuthAggregateConfig>()
                .AsImplementedInterfaces();

            // Use Vs authentication
            builder.RegisterType<VsAuthenticationProvider>()
                .AsSelf();
            // fallback to device code token provider
            builder.RegisterType<DeviceCodeTokenProvider>()
                .AsSelf();

            // Use cli token source
            builder.RegisterType<CliTokenSource>()
                .AsImplementedInterfaces().SingleInstance();
            base.Load(builder);
        }


        /// <summary>
        /// Authenticate with device token after trying app authentication.
        /// </summary>
        internal class CliTokenSource : ITokenSource {

            /// <inheritdoc/>
            public string Resource => Http.Resource.Platform;

            /// <inheritdoc/>
            public CliTokenSource(IComponentContext components) {
                _vs = components.Resolve<VsAuthenticationProvider>();
                _dc = components.Resolve<DeviceCodeTokenProvider>();
            }

            /// <inheritdoc/>
            public async Task<TokenResultModel> GetTokenForAsync(
                IEnumerable<string> scopes = null) {

                var token = await Try.Async(() => _vs.GetTokenForAsync(Resource, scopes));
                if (token != null) {
                    return token;
                }
                return await Try.Async(() => _dc.GetTokenForAsync(Resource, scopes));
            }

            /// <inheritdoc/>
            public async Task InvalidateAsync() {
                await _vs.InvalidateAsync(Resource);
                await _dc.InvalidateAsync(Resource);
            }

            private readonly VsAuthenticationProvider _vs;
            private readonly DeviceCodeTokenProvider _dc;
        }
    }
}
