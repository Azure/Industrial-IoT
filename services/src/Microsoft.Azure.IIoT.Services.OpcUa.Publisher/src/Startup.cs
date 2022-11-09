// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Publisher {
    using Autofac;
    using Autofac.Extensions.DependencyInjection;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Azure.IIoT.Agent.Framework.Jobs;
    using Microsoft.Azure.IIoT.Agent.Framework.Storage.Database;
    using Microsoft.Azure.IIoT.AspNetCore.Auth;
    using Microsoft.Azure.IIoT.AspNetCore.Auth.Clients;
    using Microsoft.Azure.IIoT.AspNetCore.Correlation;
    using Microsoft.Azure.IIoT.AspNetCore.Cors;
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Diagnostics.AppInsights.Default;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Http.Ssl;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Services;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Deploy;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Services.OpcUa.Publisher.Auth;
    using Microsoft.Azure.IIoT.Services.OpcUa.Publisher.Runtime;
    using Microsoft.Azure.IIoT.Storage.CosmosDb.Services;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.OpenApi.Models;
    using Prometheus;
    using System;
    using ILogger = Serilog.ILogger;

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
                .AddFromDotEnvFile()
                .AddEnvironmentVariables()
                .AddEnvironmentVariables(EnvironmentVariableTarget.User)
                // Above configuration providers will provide connection
                // details for KeyVault configuration provider.
                .AddFromKeyVault(providerPriority: ConfigurationProviderPriority.Lowest)
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
        /// Configure services
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public void ConfigureServices(IServiceCollection services) {

            // services.AddLogging(o => o.AddConsole().AddDebug());

            services.AddHeaderForwarding();
            services.AddCors();
            services.AddHealthChecks();
            services.AddDistributedMemoryCache();

            services.AddHttpsRedirect();
            services.AddAuthentication()
                .AddJwtBearerProvider(AuthProvider.AzureAD)
                .AddJwtBearerProvider(AuthProvider.AuthService);
            services.AddAuthorizationPolicies(
                Policies.RoleMapping,
                Policies.CanRead,
                Policies.CanWrite,
                Policies.CanPublish);

            // TODO: Remove http client factory and use
            // services.AddHttpClient();

            // Add controllers as services so they'll be resolved.
            services.AddControllers().AddSerializers();
            services.AddSwagger(ServiceInfo.Name, ServiceInfo.Description);

            // Enable Application Insights telemetry collection.
#pragma warning disable CS0618 // Type or member is obsolete
            services.AddApplicationInsightsTelemetry(Config.InstrumentationKey);
#pragma warning restore CS0618 // Type or member is obsolete
            services.AddSingleton<ITelemetryInitializer, ApplicationInsightsTelemetryInitializer>();
        }

        /// <summary>
        /// This method is called by the runtime, after the ConfigureServices
        /// method above and used to add middleware
        /// </summary>
        /// <param name="app"></param>
        /// <param name="appLifetime"></param>
        public void Configure(IApplicationBuilder app, IHostApplicationLifetime appLifetime) {
            var applicationContainer = app.ApplicationServices.GetAutofacRoot();
            var log = applicationContainer.Resolve<ILogger>();

            app.UsePathBase();
            app.UseHeaderForwarding();

            app.UseRouting();
            app.EnableCors();

            app.UseJwtBearerAuthentication();
            app.UseAuthorization();
            app.UseHttpsRedirect();

            app.UseCorrelation();
            app.UseSwagger();
            app.UseMetricServer();
            app.UseHttpMetrics();
            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/healthz");
            });

            // If you want to dispose of resources that have been resolved in the
            // application container, register for the "ApplicationStopped" event.
            appLifetime.ApplicationStopped.Register(applicationContainer.Dispose);

            // Print some useful information at bootstrap time
            log.Information("{service} web service started with id {id}",
                ServiceInfo.Name, ServiceInfo.Id);
        }

        /// <summary>
        /// Autofac configuration.
        /// </summary>
        /// <param name="builder"></param>
        public virtual void ConfigureContainer(ContainerBuilder builder) {

            // Register service info and configuration interfaces
            builder.RegisterInstance(ServiceInfo)
                .AsImplementedInterfaces();
            builder.RegisterInstance(Config)
                .AsImplementedInterfaces();
            builder.RegisterInstance(Config.Configuration)
                .AsImplementedInterfaces();

            // Add diagnostics
            builder.AddDiagnostics(Config);

            // Register http client module
            builder.RegisterModule<HttpClientModule>();
#if DEBUG
            builder.RegisterType<NoOpCertValidator>()
                .AsImplementedInterfaces();
#endif
            // Add serializers
            builder.RegisterModule<MessagePackModule>();
            builder.RegisterModule<NewtonSoftJsonModule>();

            // Add service to service authentication
            builder.RegisterModule<WebApiAuthentication>();

            // CORS setup
            builder.RegisterType<CorsSetup>()
                .AsImplementedInterfaces();

            // Twin services for browsing and tag selection ...
            builder.RegisterType<TwinServicesApiAdapter>()
                .AsImplementedInterfaces();
            builder.RegisterType<TwinServiceClient>()
                .AsImplementedInterfaces();

            // Registry services to lookup endpoints.
            builder.RegisterType<RegistryServicesApiAdapter>()
                .AsImplementedInterfaces();
            builder.RegisterType<RegistryServiceClient>()
                .AsImplementedInterfaces();

            // Create Publish jobs using ...
            builder.RegisterType<PublisherJobService>()
                .AsImplementedInterfaces();
            builder.RegisterType<PublisherJobSerializer>()
                .AsImplementedInterfaces();

            // ... job services and dependencies
            builder.RegisterType<DefaultJobService>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<JobDatabase>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<WorkerDatabase>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<CosmosDbServiceClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<IoTHubJobConfigurationHandler>()
                .AsImplementedInterfaces();
            builder.RegisterType<IoTHubServiceHttpClient>()
                .AsImplementedInterfaces();

            builder.RegisterType<IoTHubConfigurationClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<IoTHubPublisherDeployment>()
                .AsImplementedInterfaces().SingleInstance();

            // ... and auto start
            builder.RegisterType<HostAutoStart>()
                .AutoActivate()
                .AsImplementedInterfaces().SingleInstance();
        }
    }
}
