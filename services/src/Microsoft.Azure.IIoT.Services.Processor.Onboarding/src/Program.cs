// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Processor.Onboarding {

    using Autofac;
    using Autofac.Extensions.DependencyInjection;
    using Microsoft.Azure.IIoT.AspNetCore.Diagnostics.Default;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Http.Ssl;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Processor.EventHub;
    using Microsoft.Azure.IIoT.Hub.Processor.Services;
    using Microsoft.Azure.IIoT.Hub.Services;
    using Microsoft.Azure.IIoT.Messaging.Default;
    using Microsoft.Azure.IIoT.Messaging.ServiceBus.Clients;
    using Microsoft.Azure.IIoT.Messaging.ServiceBus.Services;
    using Microsoft.Azure.IIoT.Module.Default;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Handlers;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Services;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Services.Processor.Onboarding.Runtime;
    using Microsoft.Azure.IIoT.Tasks.Default;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Serilog;
    using System;

    /// <summary>
    /// IoT Hub device onboarding processor host.  Processes all
    /// events from devices including onboarding and discovery events.
    /// </summary>
    public class Program {

        /// <summary>
        /// Main entry point for iot hub device event processor host
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args) {
            CreateHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// Create host builder
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IHostBuilder CreateHostBuilder(string[] args) {
            return Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(configHost => {
                    configHost.AddFromDotEnvFile()
                    .AddEnvironmentVariables()
                    .AddEnvironmentVariables(EnvironmentVariableTarget.User)
                    // Above configuration providers will provide connection
                    // details for KeyVault configuration provider.
                    .AddFromKeyVault(providerPriority: ConfigurationProviderPriority.Lowest);
                })
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureContainer<ContainerBuilder>((hostBuilderContext, builder) => {
                    // registering services in the Autofac ContainerBuilder
                    ConfigureContainer(builder, hostBuilderContext.Configuration);
                })
                .ConfigureServices((hostBuilderContext, services) => {
                    ConfigureServices(services, hostBuilderContext.Configuration);
                })
                .UseSerilog();
        }

        /// <summary>
        /// This is where you register dependencies, add services to the container.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        public static void ConfigureServices(
            IServiceCollection services,
            IConfiguration configuration
        ) {
            services.AddHostedService<HostStarterService>();
        }

        /// <summary>
        /// Autofac configuration.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        public static ContainerBuilder ConfigureContainer(
            ContainerBuilder builder,
            IConfiguration configuration
        ) {
            var serviceInfo = new ServiceInfo();
            var config = new Config(configuration);

            builder.RegisterInstance(serviceInfo)
                .AsImplementedInterfaces();

            // Register configuration interfaces
            builder.RegisterInstance(config)
                .AsSelf()
                .AsImplementedInterfaces();
            builder.RegisterInstance(config.Configuration)
                .AsImplementedInterfaces();

            // Add Application Insights dependency tracking.
            builder.AddDependencyTracking(config, serviceInfo);

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

            // and build on Iot hub services
            builder.RegisterType<IoTHubServiceHttpClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<IoTHubTwinMethodClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<ChunkMethodClient>()
                .AsImplementedInterfaces();

            // Event processor services for onboarding consumer
            builder.RegisterType<EventProcessorHost>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<EventProcessorFactory>()
                .AsImplementedInterfaces();

            // Prometheus metric server
            builder.RegisterType<MetricServerHost>()
                .AsImplementedInterfaces().SingleInstance();

            // Handle device events
            builder.RegisterType<IoTHubDeviceEventHandler>()
                .AsImplementedInterfaces();

            // Including discovery events
            builder.RegisterType<DiscoveryEventHandler>()
                .AsImplementedInterfaces();

            // Processor for discovery events plus ...
            builder.RegisterType<DiscoveryProcessor>()
                .AsImplementedInterfaces();
            builder.RegisterType<TaskProcessor>()
                .AsImplementedInterfaces().SingleInstance();

            // the dependent registries and repositories
            builder.RegisterModule<RegistryServices>();

#if !USE_APP_DB // TODO: Decide whether when to switch
            builder.RegisterType<ApplicationTwins>()
                .AsImplementedInterfaces().SingleInstance();
#else
            // Cosmos db collection as storage
            builder.RegisterType<ItemContainerFactory>()
                .AsImplementedInterfaces();
            builder.RegisterType<CosmosDbServiceClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<ApplicationDatabase>()
                .AsImplementedInterfaces().SingleInstance();
#endif
            // which need additional registry services
            builder.RegisterType<TwinModuleCertificateClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<TwinModuleActivationClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<OnboardingClient>()
                .AsImplementedInterfaces();

            // Register event bus for event publishing
            builder.RegisterType<EventBusHost>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ServiceBusClientFactory>()
                .AsImplementedInterfaces();
            builder.RegisterType<ServiceBusEventBus>()
                .AsImplementedInterfaces().SingleInstance();

            return builder;
        }
    }
}
