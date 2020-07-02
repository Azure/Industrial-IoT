// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Processor.Telemetry.Cdm {
    using Microsoft.Azure.IIoT.Services.Processor.Telemetry.Cdm.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Cdm.Services;
    using Microsoft.Azure.IIoT.OpcUa.Cdm.Storage;
    using Microsoft.Azure.IIoT.OpcUa.Subscriber.Handlers;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Core.Messaging.EventHub;
    using Microsoft.Azure.IIoT.Hub.Processor.EventHub;
    using Microsoft.Azure.IIoT.Hub.Processor.Services;
    using Microsoft.Azure.IIoT.Storage.Datalake.Runtime;
    using Microsoft.Azure.IIoT.Storage.Datalake.Default;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Http.Ssl;
    using Microsoft.Azure.IIoT.Auth.Clients.Default;
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Autofac;
    using Autofac.Extensions.DependencyInjection;
    using Serilog;
    using System;

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

            // Add unattended and storage authentication
            builder.RegisterModule<UnattendedAuthentication>();
            builder.RegisterModule<StorageAuthentication>();

            // Event processor services for onboarding consumer
            builder.RegisterType<EventProcessorHost>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<EventProcessorFactory>()
                .AsImplementedInterfaces();

            // Handle telemetry
            builder.RegisterType<EventHubDeviceEventHandler>()
                .AsImplementedInterfaces();
            builder.RegisterType<MonitoredItemSampleModelHandler>()
                .AsImplementedInterfaces();
            builder.RegisterType<NetworkMessageModelHandler>()
                .AsImplementedInterfaces();

            // Cdm processor
            builder.RegisterType<DatalakeConfig>()
                .AsImplementedInterfaces();
            builder.RegisterType<DataLakeStorageService>()
                .AsImplementedInterfaces();
            builder.RegisterType<CdmFileStorageAdapter>()
                .AsImplementedInterfaces();
            builder.RegisterType<CsvEncoder>()
                .AsImplementedInterfaces();
            builder.RegisterType<CdmMessageProcessor>()
                .AsImplementedInterfaces();

            return builder;
        }
    }
}
