// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Alerting {
    using Microsoft.Azure.IIoT.Services.OpcUa.Alerting.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2;
    using Microsoft.Azure.IIoT.OpcUa.Security.Services;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Http.Ssl;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Messaging.Default;
    using Microsoft.Azure.IIoT.Messaging.ServiceBus.Services;
    using Microsoft.Azure.IIoT.Messaging.ServiceBus.Clients;
    using Microsoft.Extensions.Configuration;
    using Autofac;
    using AutofacSerilogIntegration;
    using Serilog;
    using System;
    using System.IO;
    using System.Runtime.Loader;
    using System.Threading.Tasks;

    /// <summary>
    /// Security alerter agent emits security alerts based on service events
    /// </summary>
    public class Program {

        /// <summary>
        /// Main entry point for security alerts agent
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
        /// Run event bus host
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static async Task RunAsync(IConfigurationRoot config) {
            var exit = false;
            while (!exit) {
                using (var container = ConfigureContainer(config).Build()) {
                    var host = container.Resolve<IHost>();
                    var logger = container.Resolve<ILogger>();
                    // Wait until the agent unloads or is cancelled
                    var tcs = new TaskCompletionSource<bool>();
                    AssemblyLoadContext.Default.Unloading += _ => tcs.TrySetResult(true);
                    try {
                        logger.Information("Starting security alerter...");
                        await host.StartAsync();
                        logger.Information("Security alerter started.");
                        exit = await tcs.Task;
                    }
                    catch (Exception ex) {
                        logger.Error(ex,
                            "Error running alerting agent - restarting!");
                    }
                    finally {
                        await host.StopAsync();
                        logger.Information("Security alerter stopped.");
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
            // Iot hub services
            builder.RegisterType<IoTHubMessagingHttpClient>()
                .AsImplementedInterfaces().SingleInstance();

            // Register event bus
            builder.RegisterType<EventBusHost>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ServiceBusClientFactory>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ServiceBusEventBus>()
                .AsImplementedInterfaces().SingleInstance();

            // ... and event subscriptions
            builder.RegisterType<ApplicationEventSubscriber>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<EndpointEventSubscriber>()
                .AsImplementedInterfaces().SingleInstance();
            // ...

            // Register alerters
            builder.RegisterType<EndpointSecurityAlerter>()
                .AsImplementedInterfaces();
            // ...

            return builder;
        }
    }
}
