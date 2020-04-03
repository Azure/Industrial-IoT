// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.Sync {
    using Microsoft.Azure.IIoT.Services.OpcUa.Registry.Sync.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Handlers;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Services;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Clients;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Http.Ssl;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Tasks.Default;
    using Microsoft.Azure.IIoT.Crypto.Default;
    using Microsoft.Azure.IIoT.Auth.IoTHub;
    using Microsoft.Azure.IIoT.Agent.Framework.Jobs;
    using Microsoft.Azure.IIoT.Module.Default;
    using Microsoft.Azure.IIoT.Messaging.Default;
    using Microsoft.Azure.IIoT.Messaging.ServiceBus.Services;
    using Microsoft.Azure.IIoT.Messaging.ServiceBus.Clients;
    using Microsoft.Extensions.Configuration;
    using Autofac;
    using Serilog;
    using System;
    using System.IO;
    using System.Runtime.Loader;
    using System.Threading.Tasks;

    /// <summary>
    /// Sync service handles jobs out of process for other services.
    /// </summary>
    public class Program {

        /// <summary>
        /// Main entry point for Sync service
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args) {

            // Load hosting configuration
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true)
                .AddEnvironmentVariables()
                .AddEnvironmentVariables(EnvironmentVariableTarget.User)
                .AddFromDotEnvFile()
                .AddFromKeyVault()
                .AddCommandLine(args)
                .Build();

            // Set up dependency injection for the event processor host
            RunAsync(config).Wait();
        }

        /// <summary>
        /// Run event bus host
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static async Task RunAsync(IConfiguration config) {
            var exit = false;
            while (!exit) {
                // Wait until the agent unloads or is cancelled
                var tcs = new TaskCompletionSource<bool>();
                AssemblyLoadContext.Default.Unloading += _ => tcs.TrySetResult(true);
                using (var container = ConfigureContainer(config).Build()) {
                    var logger = container.Resolve<ILogger>();
                    try {
                        logger.Information("Registry sync service started.");
                        exit = await tcs.Task;
                    }
                    catch (InvalidConfigurationException e) {
                        logger.Error(e,
                            "Error starting registry sync service  - exit!");
                        return;
                    }
                    catch (Exception ex) {
                        logger.Error(ex,
                            "Error running registry sync service - restarting!");
                    }
                }
            }
        }

        /// <summary>
        /// Autofac configuration.
        /// </summary>
        public static ContainerBuilder ConfigureContainer(
            IConfiguration configuration) {

            var serviceInfo = new ServiceInfo();
            var config = new Config(configuration);
            var builder = new ContainerBuilder();

            builder.RegisterInstance(serviceInfo)
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterInstance(config)
                .AsImplementedInterfaces().SingleInstance();

            // Add diagnostics based on configuration
            builder.AddDiagnostics(config);
            builder.RegisterModule<NewtonSoftJsonModule>();

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

            // Register event bus
            builder.RegisterType<EventBusHost>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ServiceBusClientFactory>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ServiceBusEventBus>()
                .AsImplementedInterfaces().SingleInstance();
            // ... and auto start
            builder.RegisterType<HostAutoStart>()
                .AutoActivate()
                .AsImplementedInterfaces().SingleInstance();

            // Register task processor
            builder.RegisterType<TaskProcessor>()
                .AsImplementedInterfaces().SingleInstance();

            // Handle discovery request and pass to all edges
            builder.RegisterModule<RegistryServices>();
            builder.RegisterType<DiscoveryRequestHandler>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<DiscoveryMultiplexer>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<DiscovererModuleClient>()
                .AsImplementedInterfaces().SingleInstance();

            // Identity token updater
            builder.RegisterType<PasswordGenerator>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<TwinIdentityTokenUpdater>()
                .AsImplementedInterfaces().SingleInstance();

            // and service endpoint sync
            builder.RegisterType<JobOrchestratorEndpointSync>()
                .AsImplementedInterfaces().SingleInstance();

            // Activation sync
            builder.RegisterType<TwinModuleActivationClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<TwinModuleCertificateClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<TwinModuleDiagnosticsClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ActivationSyncHost>()
                .AsImplementedInterfaces().SingleInstance();

            return builder;
        }
    }
}
