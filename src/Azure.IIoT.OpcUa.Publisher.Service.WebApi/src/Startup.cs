// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi
{
    using Azure.IIoT.OpcUa.Publisher.Service.WebApi.SignalR;
    using Azure.IIoT.OpcUa.Publisher.Service;
    using Azure.IIoT.OpcUa.Publisher.Service.Clients;
    using Azure.IIoT.OpcUa.Publisher.Service.Events;
    using Azure.IIoT.OpcUa.Publisher.Service.Runtime;
    using Azure.IIoT.OpcUa.Publisher.Service.Services;
    using Azure.IIoT.OpcUa.Publisher.Sdk.Publisher.Clients;
    using Azure.IIoT.OpcUa.Encoders;
    using Autofac;
    using Autofac.Extensions.DependencyInjection;
    using Furly;
    using Furly.Extensions.AspNetCore.OpenApi;
    using Furly.Extensions.Logging;
    using Furly.Tunnel.Protocol;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.OpenApi.Models;
    using Nito.AsyncEx;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

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
        /// Service name
        /// </summary>
        public static string Name => "Opc-Publisher-Service";

        /// <summary>
        /// Description
        /// </summary>
        public static string Description => "Azure Industrial IoT OPC UA Publisher Service";

        /// <summary>
        /// Create startup
        /// </summary>
        /// <param name="configuration"></param>
        public Startup(IConfiguration configuration)
        {
            Configuration = new ConfigurationBuilder()
                .AddConfiguration(configuration)
                .AddFromDotEnvFile()
                .AddEnvironmentVariables()
                .AddFromKeyVault(ConfigurationProviderPriority.Lowest)
                .Build();
        }

        /// <summary>
        /// Configure services
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
#pragma warning disable CA1822 // Mark members as static
        public void ConfigureServices(IServiceCollection services)
#pragma warning restore CA1822 // Mark members as static
        {
            services.AddLogging(options => options
                .AddConsole()
                .AddDebug())
                ;

            services.AddHeaderForwarding();
            services.AddCors();
            services.AddHealthChecks();
            services.AddMemoryCache();
            services.AddResponseCompression(options => options.EnableForHttps = true);

            // services.AddDistributedMemoryCache();
            services.AddHttpClient();
            services.AddExceptionSummarization();

            services.AddAuthentication(Configuration);
            services.AddAuthorization();
            services.AddAuthorizationBuilder()
                .AddPolicy(Policies.CanRead, options => options.RequireAuthenticatedUser())
                .AddPolicy(Policies.CanWrite, options => options.RequireAuthenticatedUser())
                .AddPolicy(Policies.CanPublish, options => options.RequireAuthenticatedUser());

            // Add controllers as services so they'll be resolved.
            services.AddControllers()
                .AddNewtonsoftSerializer()
                .AddMessagePackSerializer();

            // Add signalr and optionally configure signalr service
            services.AddSignalR()
                .AddNewtonsoftJson()
                .AddMessagePack();

            services.Configure<OpenApiOptions>(options =>
            {
                options.SchemaVersion = 2;
                options.ProjectUri = new Uri("https://www.github.com/Azure/Industrial-IoT");
                options.License = new OpenApiLicense
                {
                    Name = "MIT LICENSE",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                };
            });
            services.AddSwagger(Name, Description);
            // services.AddOpenTelemetry(Name);
            services.AddHostedService<AwaitableStartable>();
        }

        /// <summary>
        /// This method is called by the runtime, after the ConfigureServices
        /// method above and used to add middleware
        /// </summary>
        /// <param name="app"></param>
        /// <param name="appLifetime"></param>
#pragma warning disable CA1822 // Mark members as static
        public void Configure(IApplicationBuilder app, IHostApplicationLifetime appLifetime)
#pragma warning restore CA1822 // Mark members as static
        {
            app.UsePathBase();
            app.UseHeaderForwarding();

            app.UseRouting();
            app.UseResponseCompression();
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();
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
            log.LogInformation("{Service} web service started.", Name);
        }

        /// <summary>
        /// Autofac configuration.
        /// </summary>
        /// <param name="builder"></param>
        public virtual void ConfigureContainer(ContainerBuilder builder)
        {
            // Add diagnostics
            builder.RegisterInstance(IMetricsContext.Empty)
                .AsImplementedInterfaces().IfNotRegistered(typeof(IMetricsContext));
            builder.RegisterType<HealthCheckRegistrar>()
                .AsImplementedInterfaces().SingleInstance();

            // Add serializers
            builder.AddMessagePackSerializer();
            builder.AddNewtonsoftJsonSerializer();

            // Register IoT Hub services for registry, edge clients and deployment.
            builder.AddIoTHubServices();
            builder.RegisterType<PublisherDeploymentConfig>()
                .AsImplementedInterfaces();
            builder.RegisterType<PublisherDeployment>()
                .AsImplementedInterfaces();

            builder.RegisterModule<RegistryServices>();
            builder.ConfigureServices(services => services.AddMemoryCache());
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

            builder.RegisterInstance(Configuration)
               .AsImplementedInterfaces();
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
