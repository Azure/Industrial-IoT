// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Clients
{
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Azure.IIoT.Storage.Default;
    using Microsoft.Extensions.Logging;
    using Autofac;
    using System.Collections.Generic;

    /// <summary>
    /// Public native console client authentication
    /// </summary>
    public class NativeClientAuthentication : Module
    {
        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ClientAuthAggregateConfig>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<MemoryCache>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<CachingTokenProvider>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<DevAuthenticationClient>()
                .AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<AppAuthenticationClient>()
                .AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<MsalInteractiveClient>()
                .AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<MsalDeviceCodeClient>()
                .AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();

            // Use cli token source
            builder.RegisterType<NativeClientTokenSource>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            base.Load(builder);
        }

        /// <summary>
        /// Authenticate with device token after trying app and developer authentication.
        /// </summary>
        internal class NativeClientTokenSource : TokenClientAggregateSource
        {
            /// <inheritdoc/>
            public NativeClientTokenSource(DevAuthenticationClient ld, AppAuthenticationClient aa,
                MsalInteractiveClient ic, MsalDeviceCodeClient dc,
                    IEnumerable<ITokenClient> providers, ILogger<TokenClientAggregateSource> logger)
                    : base(providers, logger, ld, aa, ic, dc)
            {
            }
        }
    }
}
