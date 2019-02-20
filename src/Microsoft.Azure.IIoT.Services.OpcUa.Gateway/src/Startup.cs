// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Gateway {
    using Microsoft.Azure.IIoT.Services.OpcUa.Gateway.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Transport;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Services;
    using Microsoft.Azure.IIoT.OpcUa.Gateway.Server;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Clients;
    using Microsoft.Azure.IIoT.Services;
    using Microsoft.Azure.IIoT.Services.Diagnostics;
    using Microsoft.Azure.IIoT.Services.Auth;
    using Microsoft.Azure.IIoT.Services.Auth.Clients;
    using Microsoft.Azure.IIoT.Services.Cors;
    using Microsoft.Azure.IIoT.Http.Auth;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Module.Default;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Auth.Server.Default;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Autofac;
    using Autofac.Extensions.DependencyInjection;
    using AutofacSerilogIntegration;
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
        /// Current hosting environment - Initialized in constructor
        /// </summary>
        public IHostingEnvironment Environment { get; }

        /// <summary>
        /// Di container - Initialized in ConfigureServices
        /// </summary>
        public IContainer ApplicationContainer { get; private set; }

        /// <summary>
        /// Created through builder
        /// </summary>
        /// <param name="env"></param>
        /// <param name="configuration"></param>
        public Startup(IHostingEnvironment env, IConfiguration configuration) {
            Environment = env;
            Config = new Config(ServiceInfo.ID,
                new ConfigurationBuilder()
                    .AddConfiguration(configuration)
                    .SetBasePath(env.ContentRootPath)
                    .AddJsonFile(
                        "appsettings.json", true, true)
                    .AddJsonFile(
                        $"appsettings.{env.EnvironmentName}.json", true, true)
                    .Build());
        }

        /// <summary>
        /// This is where you register dependencies, add services to the
        /// container. This method is called by the runtime, before the
        /// Configure method below.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public IServiceProvider ConfigureServices(IServiceCollection services) {

            services.AddLogging(o => o.AddConsole().AddDebug());

            // Setup (not enabling yet) CORS
            services.AddCors();

            // Add authentication
            services.AddJwtBearerAuthentication(Config, Environment.IsDevelopment());

            // TODO: Remove http client factory and use
            // services.AddHttpClient();

            // Add controllers as services so they'll be resolved.
            services.AddMvc(options => options.Filters.Add(typeof(AuditLogFilter)))
                .AddApplicationPart(GetType().Assembly)
                .AddControllersAsServices();

            // Prepare DI container
            ApplicationContainer = ConfigureContainer(services);
            // Create the IServiceProvider based on the container
            return new AutofacServiceProvider(ApplicationContainer);
        }

        /// <summary>
        /// This method is called by the runtime, after the ConfigureServices
        /// method above and used to add middleware
        /// </summary>
        /// <param name="app"></param>
        /// <param name="appLifetime"></param>
        public void Configure(IApplicationBuilder app, IApplicationLifetime appLifetime) {

            var log = ApplicationContainer.Resolve<ILogger>();

            if (Config.AuthRequired) {
                app.UseAuthentication();
            }
            if (Config.HttpsRedirectPort > 0) {
                // app.UseHsts();
                app.UseHttpsRedirection();
            }

            app.EnableCors();
            app.UseMvc();
            app.UseOpcUaTransport();

            // If you want to dispose of resources that have been resolved in the
            // application container, register for the "ApplicationStopped" event.
            appLifetime.ApplicationStopped.Register(ApplicationContainer.Dispose);

            // Print some useful information at bootstrap time
            log.Information("{service} web service started with id {id}", ServiceInfo.NAME,
                Uptime.ProcessId);
        }

        /// <summary>
        /// Autofac configuration.
        /// </summary>
        public IContainer ConfigureContainer(IServiceCollection services) {
            var builder = new ContainerBuilder();

            // Populate from services di
            builder.Populate(services);

            // Register configuration interfaces
            builder.RegisterInstance(Config)
                .AsImplementedInterfaces().SingleInstance();

            // Register logger
            builder.RegisterLogger();

            // Diagnostics
            builder.RegisterType<AuditLogFilter>()
                .AsImplementedInterfaces().SingleInstance();

            // ... audit log to cosmos db
#if ENABLE_AUDIT_LOG
            if (Config.DbConnectionString != null) {
                builder.RegisterType<CosmosDbAuditLogWriter>()
                    .AsImplementedInterfaces().SingleInstance();
            }
#endif
            // CORS setup
            builder.RegisterType<CorsSetup>()
                .AsImplementedInterfaces().SingleInstance();

            // Register http client module
            builder.RegisterModule<HttpClientModule>();
            // ... with bearer auth
            if (Config.AuthRequired) {
                builder.RegisterType<BehalfOfTokenProvider>()
                    .AsImplementedInterfaces().SingleInstance();
                builder.RegisterType<DistributedTokenCache>()
                    .AsImplementedInterfaces().SingleInstance();
                builder.RegisterType<HttpBearerAuthentication>()
                    .AsImplementedInterfaces().SingleInstance();
            }
            builder.RegisterType<JwtTokenValidator>()
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

            // Opc Ua services
            builder.RegisterType<RegistryServices>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ActivationClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<OnboardingClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<TwinClient>()
                .AsImplementedInterfaces().SingleInstance();
            //builder.RegisterType<Twin.Clients.SupervisorClient>()
            //    .AsImplementedInterfaces().SingleInstance();

            // Auto start listeners
            builder.RegisterType<TcpChannelListener>()
                .AutoActivate()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<WebSocketChannelListener>()
                .AutoActivate()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<HttpChannelListener>()
                .AutoActivate()
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<StackLogger>()
                .AutoActivate()
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<SessionServices>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<MessageSerializer>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<JsonVariantEncoder>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<GatewayServer>()
                .AsImplementedInterfaces().SingleInstance();

            return builder.Build();
        }
    }
}
