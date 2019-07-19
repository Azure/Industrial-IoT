// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Clients;
    using Autofac;

    /// <summary>
    /// Injected twin module clients
    /// </summary>
    public sealed class TwinModuleClients : Module {

        /// <summary>
        /// Load the module
        /// </summary>
        /// <param name="builder"></param>
        protected override void Load(ContainerBuilder builder) {

            // Services
            builder.RegisterType<TwinClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<SupervisorClient>()
                .AsImplementedInterfaces().SingleInstance();

            base.Load(builder);
        }
    }
}
