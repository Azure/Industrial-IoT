// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Client {
    using Autofac;

    /// <summary>
    /// Injected iot hub service client
    /// </summary>
    public sealed class IoTHubClientModule : Module {

        /// <summary>
        /// Load the module
        /// </summary>
        /// <param name="builder"></param>
        protected override void Load(ContainerBuilder builder) {

            // Services
            builder.RegisterType<IoTHubServiceClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<IoTHubConfigurationClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<IoTHubFileNotificationHost>()
                .AsImplementedInterfaces();

            base.Load(builder);
        }
    }
}
