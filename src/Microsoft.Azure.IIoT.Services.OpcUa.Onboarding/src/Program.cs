// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Onboarding {
    using Microsoft.Azure.IIoT.Services.OpcUa.Onboarding.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Handlers;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Services;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Processor.EventHub;
    using Microsoft.Azure.IIoT.Hub.Processor.Services;
    using Microsoft.Azure.IIoT.Hub.Services;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Module.Default;
    using Microsoft.Azure.IIoT.Tasks.Default;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Http.Ssl;
    using Microsoft.Extensions.Configuration;
    using Autofac;
    using AutofacSerilogIntegration;
    using System;
    using System.IO;
    using System.Runtime.Loader;
    using System.Threading.Tasks;
    using Serilog;

    /// <summary>
    /// IoT Hub device event processor host
    /// </summary>
    public class Program {

        /// <summary>
        /// Main entry point for iot hub device event processor host
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args) {

            // Load hosting configuration
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true)
                .AddFromDotEnvFile()
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            // Set up dependency injection for the event processor host
            RunAsync(config).Wait();
        }

        /// <summary>
        /// Run
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static async Task RunAsync(IConfigurationRoot config) {
            var exit = false;
            while (!exit) {
                using (var container = ConfigureContainer(config).Build()) {
                    var host = container.Resolve<IEventProcessorHost>();
                    var logger = container.Resolve<ILogger>();
                    // Wait until the event processor host unloads or is cancelled
                    var tcs = new TaskCompletionSource<bool>();
                    AssemblyLoadContext.Default.Unloading += _ => tcs.TrySetResult(true);
                    try {
                        logger.Information("Starting event processor host...");
                        await host.StartAsync();
                        logger.Information("Event processor host started.");
                        exit = await tcs.Task;
                    }
                    catch (InvalidConfigurationException e) {
                        logger.Error(e, "Error starting event processor host - exit!");
                        return;
                    }
                    catch (Exception ex) {
                        logger.Error(ex, "Error running event processor host - restarting!");
                    }
                    finally {
                        await host.StopAsync();
                        logger.Information("Event processor host stopped.");
                    }
                }
            }
        }

        /// <summary>
        /// Autofac configuration.
        /// </summary>
        public static ContainerBuilder ConfigureContainer(
            IConfigurationRoot configuration) {

            var config = new Config(ServiceInfo.ID, configuration);
            var builder = new ContainerBuilder();

            // Register configuration interfaces
            builder.RegisterInstance(config)
                .AsImplementedInterfaces().SingleInstance();

            // register logger
            builder.RegisterLogger(LogEx.Console());

            // Register http client module
            builder.RegisterModule<HttpClientModule>();
#if DEBUG
            builder.RegisterType<NoOpCertValidator>()
                .AsImplementedInterfaces();
#endif
            // Iot hub services
            builder.RegisterType<IoTHubServiceHttpClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<IoTHubTwinMethodClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ChunkMethodClient>()
                .AsImplementedInterfaces().SingleInstance();

            // Event processor services
            builder.RegisterType<EventProcessorHost>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<EventProcessorFactory>()
                .AsImplementedInterfaces().SingleInstance();

            // Handle device events
            builder.RegisterType<IoTHubDeviceEventHandler>()
                .AsImplementedInterfaces().SingleInstance();

            // Including discovery events
            builder.RegisterType<DiscoveryEventHandler>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<DiscoveryRequestHandler>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<TaskProcessor>()
                .AsImplementedInterfaces().SingleInstance();

            // And fleet discovery and activation
            builder.RegisterType<RegistryServices>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ActivationClient>()
                .AsImplementedInterfaces().SingleInstance();
#if USE_JOBS
            builder.RegisterType<DiscoveryJobClient>()
                .AsImplementedInterfaces().SingleInstance();
#else
            builder.RegisterType<DiscoveryServices>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<DiscoveryClient>()
                .AsImplementedInterfaces().SingleInstance();
#endif
            return builder;
        }
    }
}
