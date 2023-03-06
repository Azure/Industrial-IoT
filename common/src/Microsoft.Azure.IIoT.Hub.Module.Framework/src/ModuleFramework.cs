// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework
{
    using Microsoft.Azure.IIoT.Module.Framework.Hosting;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Hub.Module.Client.Default;
    using Microsoft.Extensions.DependencyInjection;
    using Autofac;

    /// <summary>
    /// Injected module framework module
    /// </summary>
    public sealed class ModuleFramework : Module
    {
        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<EmptyMetricsContext>()
                .AsImplementedInterfaces().IfNotRegistered(typeof(IMetricsContext));

            // Register sdk and host
            builder.RegisterType<IoTSdkFactory>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ModuleHost>()
                .AsImplementedInterfaces().SingleInstance();

            // Auto wire property for circular dependency resolution
            builder.RegisterType<MethodRouter>()
                .AsImplementedInterfaces().InstancePerLifetimeScope()
                .PropertiesAutowired(
                    PropertyWiringOptions.AllowCircularDependencies);
            builder.RegisterType<SettingsRouter>()
                .AsImplementedInterfaces().InstancePerLifetimeScope()
                .PropertiesAutowired(
                    PropertyWiringOptions.AllowCircularDependencies);

            // Register edgelet client (uses http client factorys)
            builder.ConfigureServices(services => services.AddHttpClient());
            builder.RegisterType<EdgeletClient>()
                .AsImplementedInterfaces().SingleInstance();

            base.Load(builder);
        }
    }
}
