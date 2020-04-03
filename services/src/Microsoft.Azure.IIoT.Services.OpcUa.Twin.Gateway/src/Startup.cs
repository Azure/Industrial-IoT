// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.Gateway {
    using Microsoft.Azure.IIoT.Services.OpcUa.Twin.Gateway.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Gateway.Server;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Transport;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Clients;
    using Microsoft.Azure.IIoT.AspNetCore.Auth;
    using Microsoft.Azure.IIoT.AspNetCore.Auth.Clients;
    using Microsoft.Azure.IIoT.AspNetCore.ForwardedHeaders;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Http.Auth;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Module.Default;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Auth.Server.Default;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Hosting;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
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
            services.AddJwtBearerAuthentication(Config, Environment.IsDevelopment());

            // TODO: Remove http client factory and use
            // services.AddHttpClient();

            // Add controllers as services so they'll be resolved.
            services.AddControllers();
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

            if (Config.AuthRequired) {
                app.UseAuthentication();
            }
            app.UseAuthorization();
            if (Config.HttpsRedirectPort > 0) {
                app.UseHsts();
                app.UseHttpsRedirection();
            }
            app.UseMetricServer();
            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/healthz");
            });
            app.UseOpcUaTransport();

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

            // Add diagnostics based on configuration
            builder.AddDiagnostics(Config);
            builder.RegisterModule<NewtonSoftJsonModule>();

            // Register http client module
            builder.RegisterModule<HttpClientModule>();
            builder.RegisterType<HttpBearerAuthentication>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<PassThroughTokenProvider>()
                .AsImplementedInterfaces().SingleInstance();

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

            // Register registry micro service adapter
            builder.RegisterType<RegistryServiceClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<RegistryServicesApiAdapter>()
                .AsImplementedInterfaces().SingleInstance();

            // Todo: use twin micro service adapter
            builder.RegisterType<TwinModuleControlClient>()
                .AsImplementedInterfaces().SingleInstance();

            // Auto start listeners
            builder.RegisterType<TcpChannelListener>()
               // .AutoActivate()
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
            builder.RegisterType<VariantEncoderFactory>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<GatewayServer>()
                .AsImplementedInterfaces().SingleInstance();
        }
    }
}
