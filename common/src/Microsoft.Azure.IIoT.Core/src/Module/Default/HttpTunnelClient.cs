// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Default {
    using Microsoft.Azure.IIoT.Http.Default;
    using Autofac;

    /// <summary>
    /// Injected http tunnel handler
    /// </summary>
    public sealed class HttpTunnelClient : Module {

        /// <summary>
        /// Load the module
        /// </summary>
        /// <param name="builder"></param>
        protected override void Load(ContainerBuilder builder) {
            builder.RegisterModule<HttpClientModule>();

            //
            // Override default factory with configurable http tunnel
            // client handler factory.
            //
            builder.RegisterType<HttpTunnelConfigurableFactory>().SingleInstance()
                .AsImplementedInterfaces();
        }
    }
}
