// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin {
    using Microsoft.Azure.IIoT.OpcUa.Modules.Twin.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Export;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Control;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Supervisor;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Discovery;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.Module.Framework.Services;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Extensions.Configuration;
    using Autofac;
    using System;
    using System.IO;
    using System.Runtime.Loader;
    using System.Threading.Tasks;
    using Microsoft.Azure.IIoT.Tasks.Default;
    using Microsoft.Azure.IIoT.OpcUa.Edge;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher;

    /// <summary>
    /// Main entry point
    /// </summary>
    public static class Program {

        /// <summary>
        /// Main entry point to run the micro service process
        /// </summary>
        public static void Main(string[] args) {

            // Load hosting configuration
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            // Set up dependency injection for the module host
            var container = ConfigureContainer(config);
            RunAsync(container, config).Wait();
        }

        /// <summary>
        /// Run module host
        /// </summary>
        /// <param name="container"></param>
        /// <returns></returns>
        public static async Task RunAsync(IContainer container, IConfigurationRoot config) {
            using (var hostScope = container.BeginLifetimeScope()) {
                // BUGBUG: This creates 2 instances one in container one as scope
                var module = hostScope.Resolve<IModuleHost>();
                var logger = hostScope.Resolve<ILogger>();
                var exit = new TaskCompletionSource<bool>();
                AssemblyLoadContext.Default.Unloading += _ => exit.TrySetResult(true);
                while (!exit.Task.IsCompleted) {
                    // Wait until the module unloads or is cancelled
                    try {
                        var reset = new TaskCompletionSource<bool>();
                        await module.StartAsync(
                            "supervisor", config.GetValue<string>("site", null), "OpcTwin",
                                () => reset.TrySetResult(true));
                        await Task.WhenAny(reset.Task, exit.Task);
                    }
                    catch (Exception ex) {
                        logger.Error("Error during module execution - restarting!", ex);
                    }
                    finally {
                        await module.StopAsync();
                    }
                }
                logger.Info("Module exits...");
            }
        }

        /// <summary>
        /// Autofac configuration.
        /// </summary>
        public static IContainer ConfigureContainer(IConfigurationRoot configuration) {

            var config = new Config(configuration);
            var builder = new ContainerBuilder();

            // Register configuration interfaces
            builder.RegisterInstance(config)
                .AsImplementedInterfaces().SingleInstance();
            // Register logger
            builder.RegisterType<ConsoleLogger>()
                .AsImplementedInterfaces().SingleInstance();

            // Register module framework
            builder.RegisterModule<ModuleFramework>();

            // Register opc ua services
            builder.RegisterType<ClientServices>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<AddressSpaceServices>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<JsonVariantEncoder>()
                .AsImplementedInterfaces().SingleInstance();

            // Register discovery services
            builder.RegisterType<DiscoveryServices>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<TaskProcessor>()
                .AsImplementedInterfaces().SingleInstance();

            // Register controllers
            builder.RegisterType<v1.Controllers.SupervisorMethodsController>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<v1.Controllers.SupervisorSettingsController>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<v1.Controllers.DiscoverySettingsController>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();

            // Register supervisor services
            builder.RegisterType<SupervisorServices>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<TwinContainerFactory>()
                .AsImplementedInterfaces().SingleInstance();

            return builder.Build();
        }

        public class TwinContainerFactory : IContainerFactory {

            /// <summary>
            /// Create twin container factory
            /// </summary>
            /// <param name="client"></param>
            /// <param name="logger"></param>
            public TwinContainerFactory(IClientHost client, ILogger logger) {
                _client = client ?? throw new ArgumentNullException(nameof(client));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            }

            /// <inheritdoc/>
            public IContainer Create() {

                // Create container for all twin level scopes...
                var builder = new ContainerBuilder();

                // Register outer instances
                builder.RegisterInstance(_logger)
                    .AsImplementedInterfaces().SingleInstance();
                builder.RegisterInstance(_client)
                    .AsImplementedInterfaces().SingleInstance();

                // Register other opc ua services
                builder.RegisterType<JsonVariantEncoder>()
                    .AsImplementedInterfaces().SingleInstance();
                builder.RegisterType<AddressSpaceServices>()
                    .AsImplementedInterfaces().SingleInstance();
                builder.RegisterType<ModelExportServices>()
                    .AsImplementedInterfaces().InstancePerLifetimeScope();
#if PUBLISHER_MODULE_CONTROL
                builder.RegisterType<PublisherModuleServices>()
                    .AsImplementedInterfaces().InstancePerLifetimeScope();
                builder.RegisterType<PublisherModuleServerClient>()
                    .AsImplementedInterfaces().InstancePerLifetimeScope();
#else
                builder.RegisterType<PublisherServicesStub>()
                    .AsImplementedInterfaces().SingleInstance();
#endif

                // Register module framework
                builder.RegisterModule<ModuleFramework>();

                // Register twin controllers
                builder.RegisterType<v1.Controllers.TwinMethodsController>()
                    .AsImplementedInterfaces().InstancePerLifetimeScope();
                builder.RegisterType<v1.Controllers.TwinSettingsController>()
                    .AsImplementedInterfaces().InstancePerLifetimeScope();
                builder.RegisterType<v1.Controllers.NodeSettingsController>()
                    .AsImplementedInterfaces().InstancePerLifetimeScope();

                // Build twin container
                return builder.Build();
            }

            private readonly IClientHost _client;
            private readonly ILogger _logger;
        }
    }
}
