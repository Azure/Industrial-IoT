// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.Devices.Edge {
    using Microsoft.Azure.Devices.Edge.Hosting;
    using Autofac;

    /// <summary>
    /// Edge host module
    /// </summary>
    public class EdgeHostModule : Autofac.Module {

        /// <summary>
        /// Load module
        /// </summary>
        /// <param name="builder"></param>
        protected override void Load(ContainerBuilder builder) {
            // Register edge framework
            builder.RegisterType<EdgeHost>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();

            // Auto wire property for circular dependency resolution
            builder.RegisterType<MethodRouter>()
                .AsImplementedInterfaces().InstancePerLifetimeScope()
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);
            builder.RegisterType<SettingsRouter>()
                .AsImplementedInterfaces().InstancePerLifetimeScope()
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);
            base.Load(builder);
        }
    }
}
