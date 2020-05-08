// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Processor.Tunnel {
    using Microsoft.Azure.IIoT.Services.Processor.Tunnel.Runtime;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Module.Default;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Processor.EventHub;
    using Microsoft.Azure.IIoT.Hub.Processor.Services;
    using Microsoft.Azure.IIoT.Hub.Services;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Http.Ssl;
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Extensions.Configuration;
    using Autofac;
    using Serilog;
    using System;
    using System.IO;
    using System.Runtime.Loader;
    using System.Threading.Tasks;

    /// <summary>
    /// IoT Hub device telemetry event processor host.  Processes all
    /// tunnel requests from devices.
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
                .AddEnvironmentVariables()
                .AddEnvironmentVariables(EnvironmentVariableTarget.User)
                .AddFromDotEnvFile()
                .AddCommandLine(args)
                // Above configuration providers will provide connection
                // details for KeyVault configuration provider.
                .AddFromKeyVault(providerPriority: ConfigurationProviderPriority.Lowest)
                .Build();

            // Set up dependency injection for the event processor host
            RunAsync(config).Wait();
        }

        /// <summary>
        /// Run
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static async Task RunAsync(IConfiguration config) {
            var exit = false;
            while (!exit) {
                // Wait until the event processor host unloads or is cancelled
                var tcs = new TaskCompletionSource<bool>();
                AssemblyLoadContext.Default.Unloading += _ => tcs.TrySetResult(true);
                using (var container = ConfigureContainer(config).Build()) {
                    var logger = container.Resolve<ILogger>();
                    try {
                        logger.Information("Events processor host started.");
                        exit = await tcs.Task;
                    }
                    catch (InvalidConfigurationException e) {
                        logger.Error(e,
                            "Error starting events processor host - exit!");
                        return;
                    }
                    catch (Exception ex) {
                        logger.Error(ex,
                            "Error running events processor host - restarting!");
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
                .AsImplementedInterfaces();

            // Register configuration interfaces
            builder.RegisterInstance(config)
                .AsImplementedInterfaces();
            builder.RegisterInstance(config.Configuration)
                .AsImplementedInterfaces();

            // Add diagnostics
            builder.AddDiagnostics(config);

            // Register http client module
            builder.RegisterModule<HttpClientModule>();
#if DEBUG
            builder.RegisterType<NoOpCertValidator>()
                .AsImplementedInterfaces();
#endif
            // Add serializers
            builder.RegisterModule<NewtonSoftJsonModule>();

            // Add unattended authentication
            builder.RegisterModule<UnattendedAuthentication>();

            // Event processor services for onboarding consumer
            builder.RegisterType<EventProcessorHost>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<EventProcessorFactory>()
                .AsImplementedInterfaces();
            // ... and auto start
            builder.RegisterType<HostAutoStart>()
                .AutoActivate()
                .AsImplementedInterfaces().SingleInstance();

            // Handle tunnel server events
            builder.RegisterType<IoTHubDeviceEventHandler>()
                .AsImplementedInterfaces();
            builder.RegisterType<HttpTunnelServer>()
                .AsImplementedInterfaces().SingleInstance(); // TODO : no singleton?

            // which builds on Iot hub method calls for responses
            builder.RegisterType<ChunkMethodClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<IoTHubTwinMethodClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<IoTHubServiceClient>()
                .AsImplementedInterfaces();

            return builder;
        }
    }
}
