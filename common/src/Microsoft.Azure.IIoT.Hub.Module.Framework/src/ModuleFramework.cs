// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework {
    using Microsoft.Azure.IIoT.Module.Framework.Hosting;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.Module.Default;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Tasks.Default;
    using Microsoft.Azure.IIoT.Tasks;
    using Autofac;
    using Microsoft.Azure.IIoT.Http.Default;

    /// <summary>
    /// Injected module framework module
    /// </summary>
    public sealed class ModuleFramework : Module {

        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder) {

            // Register sdk and host
            builder.RegisterType<IoTSdkFactory>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<EventSourceBroker>()
                .AsImplementedInterfaces().SingleInstance();
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

            // If not already registered, register task scheduler
#if USE_DEFAULT_FACTORY
            builder.RegisterType<DefaultScheduler>()
                .AsImplementedInterfaces().SingleInstance()
                .IfNotRegistered(typeof(ITaskScheduler));
#else
            builder.RegisterType<LimitingScheduler>()
                .AsImplementedInterfaces().SingleInstance()
                .IfNotRegistered(typeof(ITaskScheduler));
#endif
            builder.RegisterModule<HttpClientModule>();
            // Register edgelet client (uses http)
            builder.RegisterType<EdgeletClient>()
                .AsImplementedInterfaces().SingleInstance();

            base.Load(builder);
        }
    }
}
