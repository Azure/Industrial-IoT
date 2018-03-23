// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.EdgeService {
    using Microsoft.Azure.IoTSolutions.OpcTwin.EdgeService.Runtime;
    using Microsoft.Azure.IoTSolutions.OpcTwin.EdgeService.Supervisor;
    using Microsoft.Azure.IoTSolutions.OpcTwin.EdgeService.Discovery;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Client;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.External;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.External.Stack;
    using Microsoft.Azure.IoTSolutions.Common.Exceptions;
    using Microsoft.Azure.Devices.Edge;
    using Microsoft.Azure.Devices.Edge.Services;
    using Microsoft.Extensions.Configuration;
    using Autofac;
    using System;
    using System.IO;
    using System.Runtime.Loader;
    using System.Threading.Tasks;
    using Microsoft.Azure.IoTSolutions.Common.Diagnostics;

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

            // Check client configuration
            var client = container.Resolve<IOpcUaClient>();
            if (client.UsesProxy) {
                throw new InvalidConfigurationException(
                    "Bad configuration - client should not be in proxy mode. " +
                    "this is likely the result of a bad services configuration. " +
                    "Update your configuration and restart.");
            }

            RunAsync(container).Wait();
        }

        /// <summary>
        /// Run
        /// </summary>
        /// <param name="container"></param>
        /// <returns></returns>
        public static async Task RunAsync(IContainer container) {
            // Wait until the module unloads or is cancelled
            var tcs = new TaskCompletionSource<bool>();
            AssemblyLoadContext.Default.Unloading += _ => tcs.TrySetResult(true);

            using (var hostScope = container.BeginLifetimeScope()) {
                var events = hostScope.Resolve<IEventEmitter>();
                try {
                    var module = hostScope.Resolve<IEdgeHost>();
                    await module.StartAsync();

                    // Report type of service
                    await events.SendAsync("type", "supervisor");
                    if (!Console.IsInputRedirected) {
                        Console.WriteLine("Press any key to exit...");
                        Console.TreatControlCAsInput = true;
                        await Task.WhenAny(tcs.Task, Task.Run(() => Console.ReadKey()));
                    }
                    else {
                        await tcs.Task;
                    }
                }
                catch (Exception ex) {
                    var logger = hostScope.Resolve<ILogger>();
                    logger.Error("Error during edge run!", () => ex);
                }
                finally {
                    await events.SendAsync("type", null);
                }
            }
        }

        /// <summary>
        /// Autofac configuration. Find more information here:
        /// @see http://docs.autofac.org/en/latest/integration/aspnetcore.html
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
            builder.RegisterType<OpcUaValidationServices>()
                .AsImplementedInterfaces();
            builder.RegisterType<OpcUaJsonVariantCodec>()
                .AsImplementedInterfaces();

            // Register discovery services
            builder.RegisterType<OpcUaDiscoveryServices>()
                .AsImplementedInterfaces();

            // Register controllers
            builder.RegisterType<v1.Controllers.OpcUaSupervisorMethods>()
                .AsImplementedInterfaces();
            builder.RegisterType<v1.Controllers.OpcUaSupervisorSettings>()
                .AsImplementedInterfaces();
            // ...

            // Register supervisor services
            builder.RegisterType<OpcUaSupervisorServices>()
                .AsImplementedInterfaces().SingleInstance();

            return builder.Build();
        }
    }
}
