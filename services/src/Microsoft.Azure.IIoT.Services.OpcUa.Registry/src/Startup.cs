// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry {
    using Microsoft.Azure.IIoT.Services.OpcUa.Registry.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Services;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Migration;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Deploy;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Clients;
    using Microsoft.Azure.IIoT.AspNetCore.Auth;
    using Microsoft.Azure.IIoT.AspNetCore.Auth.Clients;
    using Microsoft.Azure.IIoT.AspNetCore.Cors;
    using Microsoft.Azure.IIoT.AspNetCore.Correlation;
    using Microsoft.Azure.IIoT.AspNetCore.ForwardedHeaders;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Http.Auth;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Services;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Module.Default;
    using Microsoft.Azure.IIoT.Messaging.Default;
    using Microsoft.Azure.IIoT.Messaging.ServiceBus.Clients;
    using Microsoft.Azure.IIoT.Messaging.ServiceBus.Services;
    using Microsoft.Azure.IIoT.Storage.CosmosDb.Services;
    using Microsoft.Azure.IIoT.Storage.Default;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Hosting;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.OpenApi.Models;
    using Autofac;
    using Autofac.Extensions.DependencyInjection;
    using System;
    using ILogger = Serilog.ILogger;
    using Prometheus;

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
            this (env, new Config(new ConfigurationBuilder()
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

            // TODO: Remove http client factory and use
            // services.AddHttpClient();

            // Add controllers as services so they'll be resolved.
            services.AddControllers().AddSerializers();
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
            var log = applicationContainer.Resolve<ILogger>();

            if (!string.IsNullOrEmpty(Config.ServicePathBase)) {
                app.UsePathBase(Config.ServicePathBase);
            }

            if (Config.AspNetCoreForwardedHeadersEnabled) {
                // Enable processing of forwarded headers
                app.UseForwardedHeaders();
            }

            app.UseRouting();
            app.EnableCors();
            app.UseMetricServer();

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
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterInstance(Config)
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterInstance(Config.Configuration)
                .AsImplementedInterfaces().SingleInstance();

            // Add diagnostics based on configuration
            builder.AddDiagnostics(Config);
            builder.RegisterModule<MessagePackModule>();
            builder.RegisterModule<NewtonSoftJsonModule>();

            // CORS setup
            builder.RegisterType<CorsSetup>()
                .AsImplementedInterfaces().SingleInstance();

            // Register http client module
            builder.RegisterModule<HttpClientModule>();

            builder.RegisterType<HttpBearerAuthentication>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<PassThroughTokenProvider>()
                .AsImplementedInterfaces().SingleInstance();

            // Iot hub services
            builder.RegisterType<IoTHubServiceHttpClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<IoTHubMessagingHttpClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<IoTHubTwinMethodClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ChunkMethodClient>()
                .AsImplementedInterfaces().SingleInstance();

            // Register event bus for event publishing
            builder.RegisterType<EventBusHost>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ServiceBusClientFactory>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ServiceBusEventBus>()
                .AsImplementedInterfaces().SingleInstance();

            // Cosmos db collection as storage
            builder.RegisterType<ItemContainerFactory>()
                .AsImplementedInterfaces();
            builder.RegisterType<CosmosDbServiceClient>()
                .AsImplementedInterfaces();

            // Registries and repositories
            builder.RegisterModule<RegistryServices>();
            builder.RegisterType<StartupMigration>()
                .AutoActivate()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<VaultApplicationMigration>()
                .AsImplementedInterfaces().SingleInstance();
#if !USE_APP_DB // TODO: Decide whether when to switch
            builder.RegisterType<ApplicationTwins>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ApplicationRecordQuery>()
                .AsImplementedInterfaces().SingleInstance();
#else
            builder.RegisterType<ApplicationDatabase>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ApplicationTwinsMigration>()
                .AsImplementedInterfaces().SingleInstance();
#endif
            // Additional registry services
            builder.RegisterType<TwinModuleActivationClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<TwinModuleCertificateClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<TwinModuleDiagnosticsClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<OnboardingClient>()
                .AsImplementedInterfaces().SingleInstance();


            builder.RegisterType<IoTHubConfigurationClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<IoTHubEdgeBaseDeployment>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<IoTHubDiscovererDeployment>()
                .AsImplementedInterfaces().SingleInstance();

            // ... and auto start
            builder.RegisterType<HostAutoStart>()
                .AutoActivate()
                .AsImplementedInterfaces().SingleInstance();
        }
    }
}
