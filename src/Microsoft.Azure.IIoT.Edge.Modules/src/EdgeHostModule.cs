// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Edge {
    using Microsoft.Azure.IIoT.Edge.Hosting;
    using Microsoft.Azure.IIoT.Edge.Client;
    using Autofac;

    /// <summary>
    /// Edge host module
    /// </summary>
    public class EdgeHostModule : Module {

        /// <summary>
        /// Load the module
        /// </summary>
        /// <param name="builder"></param>
        protected override void Load(ContainerBuilder builder) {

            // Register edge framework
            builder.RegisterType<IoTSdkFactory>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<EdgeHost>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();

            // Auto wire property for circular dependency resolution
            builder.RegisterType<MethodRouter>()
                .AsImplementedInterfaces().InstancePerLifetimeScope()
                .PropertiesAutowired(
                    PropertyWiringOptions.AllowCircularDependencies);
            builder.RegisterType<SettingsRouter>()
                .AsImplementedInterfaces().InstancePerLifetimeScope()
                .PropertiesAutowired(
                    PropertyWiringOptions.AllowCircularDependencies);

            // If not already registered, register task scheduler
            builder.RegisterType<EdgeScheduler>()
                .AsImplementedInterfaces().SingleInstance()
                .IfNotRegistered(typeof(ITaskScheduler));

            base.Load(builder);
        }
    }
}
