// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub {
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Services;
    using Autofac;

    /// <summary>
    /// Injected iot hub services
    /// </summary>
    public sealed class IoTHubModule : Module {

        /// <summary>
        /// Load the module
        /// </summary>
        /// <param name="builder"></param>
        protected override void Load(ContainerBuilder builder) {

            // Services
            builder.RegisterType<IoTHubServiceHttpClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<IoTHubMessagingHttpClient>()
                .AsImplementedInterfaces();

            // Adapters
            builder.RegisterType<IoTHubDeviceEventHandler>()
                .AsImplementedInterfaces();
            builder.RegisterType<IoTHubTwinMethodClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<IoTHubDeviceLifecycleEventHandler>()
                .AsImplementedInterfaces();
            builder.RegisterType<IoTHubTwinChangeEventHandler>()
                .AsImplementedInterfaces();

            base.Load(builder);
        }
    }
}
