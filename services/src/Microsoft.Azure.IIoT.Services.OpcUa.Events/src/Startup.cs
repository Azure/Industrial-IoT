// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Events {
    using Microsoft.Azure.IIoT.Services.OpcUa.Events.Runtime;
    using Microsoft.Azure.IIoT.AspNetCore.Auth;
    using Microsoft.Azure.IIoT.AspNetCore.Cors;
    using Microsoft.Azure.IIoT.AspNetCore.Correlation;
    using Microsoft.Azure.IIoT.AspNetCore.ForwardedHeaders;
    using Microsoft.Azure.IIoT.Core.Messaging.EventHub;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Http.Ssl;
    using Microsoft.Azure.IIoT.Hub.Processor.Services;
    using Microsoft.Azure.IIoT.Hub.Processor.EventHub;
    using Microsoft.Azure.IIoT.Messaging.Default;
    using Microsoft.Azure.IIoT.Messaging.ServiceBus.Clients;
    using Microsoft.Azure.IIoT.Messaging.ServiceBus.Services;
    using Microsoft.Azure.IIoT.Messaging.SignalR.Services;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Subscriber.Handlers;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.OpenApi.Models;
    using Autofac;
    using Autofac.Extensions.DependencyInjection;
    using Prometheus;
    using System;
    using Microsoft.Azure.IIoT.Messaging;

    /// <summary>
    /// Webservice startup
    /// </summary>
    public class Startup {

        /// <summary>
        /// Configuration - Initialized in constructor
        /// </summary>
        public Config Config { get; }

        /// <summary>
        /// Service info - Initialized in constructor
        /// </summary>
        public ServiceInfo ServiceInfo { get; } = new ServiceInfo();

        /// <summary>
        /// Current hosting environment - Initialized in constructor
        /// </summary>
        public IWebHostEnvironment Environment { get; }

        /// <summary>
        /// Create startup
        /// </summary>
        /// <param name="env"></param>
        /// <param name="configuration"></param>
        public Startup(IWebHostEnvironment env, IConfiguration configuration) :
            this(env, new Config(new ConfigurationBuilder()
                .AddConfiguration(configuration)
                .AddEnvironmentVariables()
                .AddEnvironmentVariables(EnvironmentVariableTarget.User)
                .AddFromDotEnvFile()
                .AddFromKeyVault()
                .Build())) {
        }

        /// <summary>
        /// Create startup
        /// </summary>
        /// <param name="env"></param>
        /// <param name="configuration"></param>
        public Startup(IWebHostEnvironment env, Config configuration) {
            Environment = env;
            Config = configuration;
        }

        /// <summary>
        /// This is where you register dependencies, add services to the
        /// container. This method is called by the runtime, before the
        /// Configure method below.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public void ConfigureServices(IServiceCollection services) {

            services.AddLogging(o => o.AddConsole().AddDebug());

            if (Config.AspNetCoreForwardedHeadersEnabled) {
                // Configure processing of forwarded headers
                services.ConfigureForwardedHeaders(Config);
            }

            // Setup (not enabling yet) CORS
            services.AddCors();
            services.AddHealthChecks();
            services.AddDistributedMemoryCache();

            // Add authentication
            services.AddJwtBearerAuthentication(Config,
                Environment.IsDevelopment());

            // Add authorization
            services.AddAuthorization(options => {
                options.AddPolicies(Config.AuthRequired,
                    Config.UseRoles && !Environment.IsDevelopment());
            });

            // Add controllers as services so they'll be resolved.
            services.AddControllers().AddSerializers();

            // Add signalr and optionally configure signalr service
            services.AddSignalR()
                .AddJsonSerializer()
                .AddMessagePackSerializer()
                .AddAzureSignalRService(Config);

            services.AddSwagger(Config, ServiceInfo.Name, ServiceInfo.Description);
        }

        /// <summary>
        /// This method is called by the runtime, after the ConfigureServices
        /// method above and used to add middleware
        /// </summary>
        /// <param name="app"></param>
        /// <param name="appLifetime"></param>
        public void Configure(IApplicationBuilder app, IHostApplicationLifetime appLifetime) {
            var applicationContainer = app.ApplicationServices.GetAutofacRoot();

            if (!string.IsNullOrEmpty(Config.ServicePathBase)) {
                app.UsePathBase(Config.ServicePathBase);
            }

            if (Config.AspNetCoreForwardedHeadersEnabled) {
                // Enable processing of forwarded headers
                app.UseForwardedHeaders();
            }

            app.UseRouting();
            app.EnableCors();

            if (Config.AuthRequired) {
                app.UseAuthentication();
            }
            app.UseAuthorization();
            if (Config.HttpsRedirectPort > 0) {
                app.UseHsts();
                app.UseHttpsRedirection();
            }

            app.UseCorrelation();
            app.UseSwagger();
            app.UseMetricServer();
            app.UseEndpoints(endpoints => {
                endpoints.MapHubs();
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/healthz");
            });

            // If you want to dispose of resources that have been resolved in the
            // application container, register for the "ApplicationStopped" event.
            appLifetime.ApplicationStopped.Register(applicationContainer.Dispose);
        }

        /// <summary>
        /// Autofac configuration.
        /// </summary>
        /// <param name="builder"></param>
        public virtual void ConfigureContainer(ContainerBuilder builder) {

            builder.RegisterInstance(Config)
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterInstance(Config.Configuration)
                .AsImplementedInterfaces().SingleInstance();

            // Register logger
            builder.AddDiagnostics(Config);
            builder.RegisterModule<MessagePackModule>();
            builder.RegisterModule<NewtonSoftJsonModule>();

            // Register metrics logger
            builder.RegisterType<MetricsLogger>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<CorsSetup>()
                .AsImplementedInterfaces().SingleInstance();

            // Application event hub
            builder.RegisterType<SignalRHub<ApplicationsHub>>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<
                ApplicationEventForwarder<ApplicationsHub>>()
                .AsImplementedInterfaces().SingleInstance();

            // Endpoints event hub
            builder.RegisterType<SignalRHub<EndpointsHub>>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<
                EndpointEventForwarder<EndpointsHub>>()
                .AsImplementedInterfaces().SingleInstance();

            // Gateways event hub
            builder.RegisterType<SignalRHub<GatewaysHub>>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<
                GatewayEventForwarder<GatewaysHub>>()
                .AsImplementedInterfaces().SingleInstance();

            // Twin event hub
            builder.RegisterType<SignalRHub<SupervisorsHub>>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<
                SupervisorEventForwarder<SupervisorsHub>>()
                .AsImplementedInterfaces().SingleInstance();

            // Publishers event hub
            builder.RegisterType<SignalRHub<PublishersHub>>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<
                PublisherEventForwarder<PublishersHub>>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<
                MonitoredItemMessagePublisher<PublishersHub>>()
                .AsImplementedInterfaces().SingleInstance();

            // Discovery event hub
            builder.RegisterType<SignalRHub<DiscoverersHub>>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<
                DiscoveryProgressForwarder<DiscoverersHub>>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<
                DiscovererEventForwarder<DiscoverersHub>>()
                .AsImplementedInterfaces().SingleInstance();

            // Register http client module
            builder.RegisterModule<HttpClientModule>();
#if DEBUG
            builder.RegisterType<NoOpCertValidator>()
                .AsImplementedInterfaces();
#endif

            // Register event bus for integration events
            builder.RegisterType<EventBusHost>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ServiceBusClientFactory>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ServiceBusEventBus>()
                .AsImplementedInterfaces().SingleInstance()
                .IfNotRegistered(typeof(IEventBus));

            // Register event processor host for telemetry
            builder.RegisterType<EventProcessorHost>()
                .AsImplementedInterfaces().SingleInstance()
                .IfNotRegistered(typeof(IEventProcessingHost));
            builder.RegisterType<EventProcessorFactory>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<EventHubDeviceEventHandler>()
                .AsImplementedInterfaces().SingleInstance();

            // Handle opc-ua pub/sub telemetry subscriptions ...
            builder.RegisterType<MonitoredItemSampleModelHandler>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<NetworkMessageModelHandler>()
                .AsImplementedInterfaces().SingleInstance();

            // ... and auto start
            builder.RegisterType<HostAutoStart>()
                .AutoActivate()
                .AsImplementedInterfaces().SingleInstance();
        }
    }
}