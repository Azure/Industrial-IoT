// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Onboarding {
    using Microsoft.Azure.IIoT.Services.OpcUa.Onboarding.Runtime;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Handlers;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Services;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub.Processor.EventHub;
    using Microsoft.Azure.IIoT.Hub.Processor.Services;
    using Microsoft.Azure.IIoT.Hub.Services;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Messaging.Default;
    using Microsoft.Azure.IIoT.Messaging.ServiceBus.Clients;
    using Microsoft.Azure.IIoT.Messaging.ServiceBus.Services;
    using Microsoft.Azure.IIoT.Module.Default;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Http.Ssl;
    using Microsoft.Azure.IIoT.Storage.Default;
    using Microsoft.Azure.IIoT.Storage.CosmosDb.Services;
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
                    var host = container.Resolve<IHost>();
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

            var serviceInfo = new ServiceInfo();
            var config = new Config(configuration);
            var builder = new ContainerBuilder();

            builder.RegisterInstance(serviceInfo)
                .AsImplementedInterfaces().SingleInstance();

            // Register configuration interfaces
            builder.RegisterInstance(config)
                .AsImplementedInterfaces().SingleInstance();

            // register logger
            builder.RegisterLogger(LogEx.ApplicationInsights(config, configuration));
            // Register metrics logger
            builder.RegisterType<MetricLogger>()
                .AsImplementedInterfaces().SingleInstance();

            // Register http client module
            builder.RegisterModule<HttpClientModule>();
#if DEBUG
            builder.RegisterType<NoOpCertValidator>()
                .AsImplementedInterfaces();
#endif
            // Cosmos db storage
            builder.RegisterType<ItemContainerFactory>()
                .AsImplementedInterfaces();
            builder.RegisterType<CosmosDbServiceClient>()
                .AsImplementedInterfaces();

            // Iot hub services
            builder.RegisterType<IoTHubServiceHttpClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<IoTHubTwinMethodClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ChunkMethodClient>()
                .AsImplementedInterfaces().SingleInstance();

            // Event processor services for onboarding consumer
            builder.RegisterType<EventProcessorHost>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<EventProcessorFactory>()
                .AsImplementedInterfaces().SingleInstance();

            // Handle discovery events from opc twin module
            builder.RegisterType<IoTHubDeviceEventHandler>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<DiscoveryEventHandler>()
                .AsImplementedInterfaces().SingleInstance();

            // Register event bus to publish events
            builder.RegisterType<EventBusHost>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ServiceBusClientFactory>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ServiceBusEventBus>()
                .AsImplementedInterfaces().SingleInstance();

            // Registries and repositories
            builder.RegisterModule<RegistryServices>();
#if !USE_APP_DB // TODO: Decide whether when to switch
            builder.RegisterType<ApplicationTwins>()
                .AsImplementedInterfaces().SingleInstance();
#else
            builder.RegisterType<ApplicationDatabase>()
                .AsImplementedInterfaces().SingleInstance();
#endif
            // Finally add discovery processing and activation
            builder.RegisterType<DiscoveryProcessor>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ActivationClient>()
                .AsImplementedInterfaces().SingleInstance();

            return builder;
        }
    }
}
