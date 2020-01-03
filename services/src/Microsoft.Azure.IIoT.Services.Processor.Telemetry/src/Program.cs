// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Processor.Telemetry {
    using Microsoft.Azure.IIoT.Services.Processor.Telemetry.Runtime;
    using Microsoft.Azure.IIoT.Messaging.SignalR.Services;
    using Microsoft.Azure.IIoT.Messaging.EventHub.Services;
    using Microsoft.Azure.IIoT.Messaging.EventHub.Runtime;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.OpcUa.Api.Onboarding;
    using Microsoft.Azure.IIoT.OpcUa.Api.Onboarding.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Handlers;
    using Microsoft.Azure.IIoT.OpcUa.Subscriber.Handlers;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Processor.EventHub;
    using Microsoft.Azure.IIoT.Hub.Processor.Services;
    using Microsoft.Azure.IIoT.Hub.Services;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Http.Auth;
    using Microsoft.Azure.IIoT.Auth.Clients.Default;
    using Microsoft.Azure.IIoT.Cdm.Services;
    using Microsoft.Azure.IIoT.Cdm.Storage;
    using Microsoft.Extensions.Configuration;
    using Autofac;
    using Serilog;
    using System;
    using System.IO;
    using System.Runtime.Loader;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// IoT Hub device telemetry event processor host.  Processes all
    /// telemetry from devices - forwards unknown telemetry on to
    /// time series event hub.
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
                .AddFromKeyVault()
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
        public static async Task RunAsync(IConfiguration config) {
            var exit = false;
            while (!exit) {
                // Wait until the event processor host unloads or is cancelled
                var tcs = new TaskCompletionSource<bool>();
                AssemblyLoadContext.Default.Unloading += _ => tcs.TrySetResult(true);
                using (var container = ConfigureContainer(config).Build()) {
                    var logger = container.Resolve<ILogger>();
                    try {
                        logger.Information("Telemetry event processor host started.");
                        exit = await tcs.Task;
                    }
                    catch (InvalidConfigurationException e) {
                        logger.Error(e,
                            "Error starting telemetry event processor host - exit!");
                        return;
                    }
                    catch (Exception ex) {
                        logger.Error(ex,
                            "Error running telemetry event processor host - restarting!");
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

            // Register configuration interfaces
            builder.RegisterInstance(config)
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterInstance(config.Configuration)
                .AsImplementedInterfaces().SingleInstance();

            // register diagnostics
            builder.AddDiagnostics(config);

            // Event processor services for onboarding consumer
            builder.RegisterType<EventProcessorHost>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<EventProcessorFactory>()
                .AsImplementedInterfaces().SingleInstance();
            // ... and auto start
            builder.RegisterType<HostAutoStart>()
                .AutoActivate()
                .AsImplementedInterfaces().SingleInstance();

            // Handle telemetry events
            builder.RegisterType<IoTHubDeviceEventHandler>()
                .AsImplementedInterfaces().SingleInstance();

            // Handle discovery events
            builder.RegisterType<DiscoveryEventHandler>()
                .AsImplementedInterfaces().SingleInstance();
            // ... requires the corresponding services
            // Register http client module (needed for api)
            builder.RegisterModule<HttpClientModule>();
            // Use bearer authentication
            builder.RegisterType<HttpBearerAuthentication>()
                .AsImplementedInterfaces().SingleInstance();
            // Use device code token provider to get tokens
            builder.RegisterType<AppAuthenticationProvider>()
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<OnboardingAdapter>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<OnboardingServiceClient>()
                .AsImplementedInterfaces().SingleInstance();

            // Handle discovery messages
            builder.RegisterType<DiscoveryMessageHandler>()
                .AsImplementedInterfaces().SingleInstance();
            // ... forward progress directly to clients
            builder.RegisterType<DiscoveryProgressPublisher>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<SignalRServiceHost>()
                .AsImplementedInterfaces().SingleInstance();

            // Handle opc-ua pub/sub subscriber messages
            builder.RegisterType<MonitoredItemSampleHandler>()
                .AsImplementedInterfaces().SingleInstance();
            // ... forward samples to clients
            builder.RegisterType<MonitoredItemMessagePublisher>()
                .AsImplementedInterfaces().SingleInstance();
            // ... forward samples to the tsi eventhub
            builder.RegisterType<MonitoredItemSampleForwarder>()
                .AsImplementedInterfaces().SingleInstance();

            // Handle the CDM handler
            builder.RegisterType<AdlsCsvStorage>()
                .AsImplementedInterfaces().SingleInstance();
              // handlers for the legacy publisher (disabled)
//            builder.RegisterType<SubscriberCdmSampleHandler>()
//                .AsImplementedInterfaces().SingleInstance();
//            builder.RegisterType<SubscriberSampleCdmProcessor>()
//                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<CdmMessageProcessor>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<MonitoredItemSampleCdmProcessor>()
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<EventHubNamespaceClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<EventHubClientConfig>()
                .AsImplementedInterfaces().SingleInstance();

            // ... forward unknown samples to the tsi eventhub
            builder.RegisterType<UnknownTelemetryForwarder>()
                .AsImplementedInterfaces().SingleInstance();

            return builder;
        }

        /// <summary>
        /// Forwards telemetry not part of the platform for example from other devices
        /// </summary>
        internal sealed class UnknownTelemetryForwarder : IUnknownEventHandler, IDisposable {

            /// <summary>
            /// Create forwarder
            /// </summary>
            /// <param name="queue"></param>
            public UnknownTelemetryForwarder(IEventQueueService queue) {
                if (queue == null) {
                    throw new ArgumentNullException(nameof(queue));
                }
                _client = queue.OpenAsync().Result;
            }

            /// <inheritdoc/>
            public void Dispose() {
                _client.Dispose();
            }

            /// <inheritdoc/>
            public Task HandleAsync(byte[] eventData, IDictionary<string, string> properties) {
                return _client.SendAsync(eventData, properties);
            }

            private readonly IEventQueueClient _client;
        }
    }
}
