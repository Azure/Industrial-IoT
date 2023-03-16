// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.WebApi
{
    using Azure.IIoT.OpcUa.Services.WebApi.Auth;
    using Azure.IIoT.OpcUa.Services;
    using Azure.IIoT.OpcUa.Services.Clients;
    using Azure.IIoT.OpcUa.Services.Events;
    using Azure.IIoT.OpcUa.Services.Registry;
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Publisher.Sdk.Publisher.Clients;
    using Autofac;
    using Autofac.Extensions.DependencyInjection;
    using Furly.Tunnel.Protocol;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Azure.IIoT.AspNetCore.Auth;
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Messaging.SignalR.Services;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.OpenApi.Models;
    using System;
    using Nito.AsyncEx;
    using System.Collections;
    using Furly;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Collections.Generic;

    /// <summary>
    /// Webservice startup
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Configuration - Initialized in constructor
        /// </summary>
        public IConfigurationRoot Configuration { get; }

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
        public Startup(IWebHostEnvironment env, IConfiguration configuration)
        {
            Environment = env;
            Configuration = new ConfigurationBuilder()
                .AddConfiguration(configuration)
                .AddFromDotEnvFile()
                .AddEnvironmentVariables()
                .AddEnvironmentVariables(EnvironmentVariableTarget.User)
                .AddFromKeyVault(ConfigurationProviderPriority.Lowest)
                .Build();
        }

        /// <summary>
        /// Configure services
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(o => o.AddConsole().AddDebug());

            services.AddHeaderForwarding();
            services.AddCors();
            services.AddHealthChecks();
            services.AddDistributedMemoryCache();

            services.AddHttpsRedirect();
            services.AddHttpClient();

            services.AddIoTHubServices();

            services.AddAuthentication()
                .AddJwtBearerProvider(AuthProvider.AzureAD);

            services.AddAuthorizationPolicies(
                Policies.RoleMapping,
                Policies.CanRead,
                Policies.CanWrite,
                Policies.CanPublish);

            // Add controllers as services so they'll be resolved.
            services.AddControllers()
                .AddNewtonsoftSerializer()
                .AddMessagePackSerializer();

            // Add signalr and optionally configure signalr service
            services.AddSignalR()
                .AddJsonSerializer()
                .AddMessagePackSerializer();

            services.AddSwagger(ServiceInfo.Name, ServiceInfo.Description);
            // services.AddOpenTelemetry(ServiceInfo.Name);
            services.AddHostedService<AwaitableStartable>();
        }

        /// <summary>
        /// This method is called by the runtime, after the ConfigureServices
        /// method above and used to add middleware
        /// </summary>
        /// <param name="app"></param>
        /// <param name="appLifetime"></param>
        public void Configure(IApplicationBuilder app, IHostApplicationLifetime appLifetime)
        {
            app.UsePathBase();
            app.UseHeaderForwarding();

            app.UseRouting();
            app.UseCors();

            app.UseJwtBearerAuthentication();
            app.UseAuthorization();
            app.UseHttpsRedirect();

            app.UseSwagger();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHubs();
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/healthz");
            });

            // app.UsePrometheus();

            var applicationContainer = app.ApplicationServices.GetAutofacRoot();
            var log = applicationContainer.Resolve<ILogger<Startup>>();
            appLifetime.ApplicationStopped.Register(applicationContainer.Dispose);
            log.LogInformation("{Service} web service started with id {Id}",
                ServiceInfo.Name, ServiceInfo.Id);
        }

        /// <summary>
        /// Autofac configuration.
        /// </summary>
        /// <param name="builder"></param>
        public virtual void ConfigureContainer(ContainerBuilder builder)
        {
            // Register service info and configuration
            builder.RegisterInstance(ServiceInfo)
                .AsImplementedInterfaces();
            builder.RegisterInstance(Configuration)
                .AsImplementedInterfaces();

            // Add diagnostics
            builder.AddDiagnostics();

            // Add serializers
            builder.AddMessagePackSerializer();
            builder.AddNewtonsoftJsonSerializer();

            // Register IoT Hub services for registry and edge clients.
            builder.AddIoTHubServices();
            builder.RegisterModule<RegistryServices>();

            builder.RegisterType<ChunkMethodClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<PublisherServicesClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<DiscoveryServicesClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<VariantEncoderFactory>()
                .AsImplementedInterfaces();

            // Register handlers for registry events and device telemetry ...
            builder.RegisterModule<EventHandlers>();
            builder.RegisterType<DiscoveryProcessor>()
                .AsImplementedInterfaces();

            // We use Signalr hubs for registry and telemetry subscriptions
            builder.RegisterType<SignalRHub<ApplicationsHub>>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ApplicationEventPublisher<ApplicationsHub>>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<SignalRHub<EndpointsHub>>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<EndpointEventPublisher<EndpointsHub>>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<SignalRHub<GatewaysHub>>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<GatewayEventPublisher<GatewaysHub>>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<SignalRHub<SupervisorsHub>>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<SupervisorEventPublisher<SupervisorsHub>>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<SignalRHub<DiscoverersHub>>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<DiscovererEventPublisher<DiscoverersHub>>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<SignalRHub<PublishersHub>>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<PublisherEventPublisher<PublishersHub>>()
                .AsImplementedInterfaces().SingleInstance();

            // Also publish discovery progress and telemetry messages to Hubs
            builder.RegisterType<TelemetryEventPublisher<PublishersHub>>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<DiscoveryProgressPublisher<DiscoverersHub>>()
                .AsImplementedInterfaces().SingleInstance();
        }

        internal sealed class AwaitableStartable : IHostedService
        {
            /// <inheritdoc/>
            public AwaitableStartable(IEnumerable<IAwaitable> awaitables)
            {
                _awaitables = awaitables;
            }

            /// <inheritdoc/>
            public async Task StartAsync(CancellationToken cancellationToken)
            {
                await _awaitables.WhenAll().ConfigureAwait(false);
            }

            /// <inheritdoc/>
            public Task StopAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            private readonly IEnumerable<IAwaitable> _awaitables;
        }
    }
}
