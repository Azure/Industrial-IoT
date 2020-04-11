// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.KeyVault.Clients {
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Utils;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Autofac;

    /// <summary>
    /// Keyvault client and authentication support
    /// </summary>
    public class KeyVaultClientModule : Module {

        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder) {
            builder.RegisterType<KeyVaultServiceClient>()
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<ClientAuthAggregateConfig>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<MsiClientConfig>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<AadSpClientConfig>()
                .AsImplementedInterfaces().SingleInstance();

            // Use msi auth
            builder.RegisterType<MsiAuthenticationProvider>()
                .AsSelf().AsImplementedInterfaces();
            // Fall back
            builder.RegisterType<AppAuthenticationProvider>()
                .AsSelf().AsImplementedInterfaces();

            // Use service to service token source
            builder.RegisterType<KeyVaultTokenSource>()
                .AsImplementedInterfaces().SingleInstance();
            base.Load(builder);
        }

        /// <summary>
        /// Keyvault token source
        /// </summary>
        internal class KeyVaultTokenSource : ITokenSource {

            /// <inheritdoc/>
            public string Resource => Http.Resource.KeyVault;

            /// <inheritdoc/>
            public KeyVaultTokenSource(IComponentContext components) {
                _ma = components.Resolve<MsiAuthenticationProvider>();
                _aa = components.Resolve<AppAuthenticationProvider>();
            }

            /// <inheritdoc/>
            public async Task<TokenResultModel> GetTokenForAsync(
                IEnumerable<string> scopes = null) {
                var token = await Try.Async(() => _ma.GetTokenForAsync(Resource, scopes));
                if (token != null) {
                    return token;
                }
                return await Try.Async(() => _aa.GetTokenForAsync(Resource, scopes));
            }

            /// <inheritdoc/>
            public async Task InvalidateAsync() {
                await Try.Async(() => _ma.InvalidateAsync(Resource));
                await Try.Async(() => _aa.InvalidateAsync(Resource));
            }

            private readonly AppAuthenticationProvider _aa;
            private readonly MsiAuthenticationProvider _ma;
        }
    }
}
