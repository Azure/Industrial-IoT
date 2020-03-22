// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Mock {
    using Microsoft.Azure.IIoT.Module.Framework.Hosting;
    using Microsoft.Azure.IIoT.Tasks;
    using Microsoft.Azure.IIoT.Tasks.Default;
    using Microsoft.Azure.IIoT.Serializers;
    using Autofac;

    /// <summary>
    /// Injected mock edge framework module
    /// </summary>
    public sealed class IoTHubMockModule : Module {

        /// <summary>
        /// Load the module
        /// </summary>
        /// <param name="builder"></param>
        protected override void Load(ContainerBuilder builder) {

            // Module and device client simulation
            builder.RegisterType<IoTHubClientFactory>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();

            // Module host
            builder.RegisterType<ModuleHost>()
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

            // If not already registered, register a task scheduler
            builder.RegisterType<DefaultScheduler>()
                .AsImplementedInterfaces().SingleInstance()
                .IfNotRegistered(typeof(ITaskScheduler));

            // Register default serializers...
            builder.RegisterModule<NewtonSoftJsonModule>();
            base.Load(builder);
        }
    }
}
