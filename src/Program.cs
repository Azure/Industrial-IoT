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
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Stack;
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
                .AddCommandLine(args)
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json", true)
                .Build();

            // Set up dependency injection for the module host
            var container = ConfigureContainer(config);
            RunAsync(container, config).Wait();
        }

        /// <summary>
        /// Run
        /// </summary>
        /// <param name="container"></param>
        /// <returns></returns>
        public static async Task RunAsync(IContainer container, IConfigurationRoot config) {
            // Wait until the module unloads or is cancelled
            var tcs = new TaskCompletionSource<bool>();
            AssemblyLoadContext.Default.Unloading += _ => tcs.TrySetResult(true);

            using (var hostScope = container.BeginLifetimeScope()) {
                // BUGBUG: This creates 2 instances one in container one as scope
                var events = hostScope.Resolve<IEventEmitter>();
                var module = hostScope.Resolve<IEdgeHost>();
                while (true) {
                    try {
                        await module.StartAsync(
                            "supervisor", config.GetValue<string>("site", null));
#if DEBUG
                        if (!Console.IsInputRedirected) {
                            Console.WriteLine("Press any key to exit...");
                            Console.TreatControlCAsInput = true;
                            await Task.WhenAny(tcs.Task, Task.Run(() => Console.ReadKey()));
                            return;
                        }
#endif
                        await tcs.Task;
                        return;
                    }
                    catch (Exception ex) {
                        var logger = hostScope.Resolve<ILogger>();
                        logger.Error("Error during edge run - restarting!", () => ex);
                    }
                    finally {
                        await module.StopAsync();
                    }
                }
            }
        }

        /// <summary>
        /// Autofac configuration. Find more information here:
        /// see http://docs.autofac.org/en/latest/integration/aspnetcore.html
        /// </summary>
        public static IContainer ConfigureContainer(IConfigurationRoot configuration) {

            var config = new Config(configuration);
            var builder = new ContainerBuilder();

            // Register logger
            builder.RegisterInstance(config.Logger)
                .AsImplementedInterfaces().SingleInstance();
            // Register configuration interfaces
            builder.RegisterInstance(config)
                .AsImplementedInterfaces().SingleInstance();

            // Register edge framework
            builder.RegisterModule<EdgeHostModule>();

            // Register opc ua client
            builder.RegisterType<OpcUaClient>()
                .AsImplementedInterfaces().SingleInstance();

            // Register opc ua services
            builder.RegisterType<OpcUaNodeServices>()
                .AsImplementedInterfaces();
            builder.RegisterType<OpcUaDiscoveryServices>()
                .AsImplementedInterfaces();
            builder.RegisterType<OpcUaJsonVariantCodec>()
                .AsImplementedInterfaces();

            // Register discovery services
            builder.RegisterType<OpcUaDiscoveryServices>()
                .AsImplementedInterfaces();
            builder.RegisterType<TaskProcessor>()
                .AsImplementedInterfaces().SingleInstance();

            // Register controllers
            builder.RegisterType<v1.Controllers.OpcUaSupervisorMethods>()
                .AsImplementedInterfaces();
            builder.RegisterType<v1.Controllers.OpcUaSupervisorSettings>()
                .AsImplementedInterfaces();
            builder.RegisterType<v1.Controllers.OpcUaDiscoverySettings>()
                .AsImplementedInterfaces();

            // Register supervisor services
            builder.RegisterType<OpcUaSupervisorServices>()
                .AsImplementedInterfaces().SingleInstance();

            // ... and associated twin controllers...
            builder.RegisterInstance<Action<ContainerBuilder>>(b => {
                // Register twin controllers for scoped host instance
                b.RegisterType<v1.Controllers.OpcUaTwinMethods>()
                    .AsImplementedInterfaces().InstancePerLifetimeScope();
                b.RegisterType<v1.Controllers.OpcUaTwinSettings>()
                    .AsImplementedInterfaces().InstancePerLifetimeScope();
                b.RegisterType<v1.Controllers.OpcUaNodeSettings>()
                    .AsImplementedInterfaces().InstancePerLifetimeScope();
            });

            return builder.Build();
        }
    }
}
