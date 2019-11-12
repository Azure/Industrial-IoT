// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Jobs.Edge {
    using Microsoft.Azure.IIoT.Services.Common.Jobs.Edge.Runtime;
    using Microsoft.Azure.IIoT.Services.Common.Jobs.Edge.v2;
    using Microsoft.Azure.IIoT.Agent.Framework.Jobs;
    using Microsoft.Azure.IIoT.Agent.Framework.Storage.Database;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Auth;
    using Microsoft.Azure.IIoT.Services;
    using Microsoft.Azure.IIoT.Services.Auth.Clients;
    using Microsoft.Azure.IIoT.Services.Cors;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Http.Auth;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Storage.CosmosDb.Services;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Autofac;
    using Autofac.Extensions.DependencyInjection;
    using Swashbuckle.AspNetCore.Swagger;
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
        public ServiceInfo ServiceInfo { get; }

        /// <summary>
        /// Hosting environment
        /// </summary>
        public IHostingEnvironment Environment { get; }

        /// <summary>
        /// Autofac container
        /// </summary>
        public IContainer ApplicationContainer { get; private set; }

        /// <summary>
        /// Create startup
        /// </summary>
        /// <param name="env"></param>
        public Startup(IHostingEnvironment env) {
            Environment = env;
            ServiceInfo = new ServiceInfo();
            Config = new Config(
                new ConfigurationBuilder()
                    .SetBasePath(env.ContentRootPath)
                    .AddJsonFile(
                        "appsettings.json", true, true)
                    .AddJsonFile(
                        $"appsettings.{env.EnvironmentName}.json", true, true)
                    .AddEnvironmentVariables()
                    .AddEnvironmentVariables(EnvironmentVariableTarget.User)
                    .AddFromDotEnvFile()
                    .Build());
        }

        /// <summary>
        /// Configure services
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public IServiceProvider ConfigureServices(IServiceCollection services) {

            services.AddLogging(o => o.AddConsole().AddDebug());

            // Setup (not enabling yet) CORS
            services.AddCors();

            services.AddMvc()
                .AddApplicationPart(GetType().Assembly)
                .AddControllersAsServices()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            // TODO: Remove http client factory and use
            // services.AddHttpClient();

            services.AddHttpContextAccessor();
            services.AddAuthentication("DeviceTokenAuth")
                .AddScheme<AuthenticationSchemeOptions, IdentityTokenAuthenticationHandler>(
                    "DeviceTokenAuth", null);
            services.AddDistributedMemoryCache();

            services.AddSwagger(Config, new Info {
                Title = ServiceInfo.Name,
                Version = VersionInfo.PATH,
                Description = ServiceInfo.Description,
            });

            // Prepare DI container
            var builder = new ContainerBuilder();
            builder.Populate(services);
            ConfigureContainer(builder);
            ApplicationContainer = builder.Build();
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

            app.EnableCors();

            if (Config.AuthRequired) {
                app.UseAuthentication();
            }
            if (Config.HttpsRedirectPort > 0) {
                // app.UseHsts();
                app.UseHttpsRedirection();
            }

            app.UseSwagger(Config, new Info {
                Title = ServiceInfo.Name,
                Version = VersionInfo.PATH,
                Description = ServiceInfo.Description,
            });

            app.UseMvc();

            // If you want to dispose of resources that have been resolved in the
            // application container, register for the "ApplicationStopped" event.
            appLifetime.ApplicationStopped.Register(ApplicationContainer.Dispose);

            // Print some useful information at bootstrap time
            log.Information("{service} web service started with id {id}", ServiceInfo.Name,
                Uptime.ProcessId);
        }

        /// <summary>
        /// Configure Autofac container
        /// </summary>
        /// <param name="builder"></param>
        public void ConfigureContainer(ContainerBuilder builder) {
            // Register service info and configuration interfaces
            builder.RegisterInstance(ServiceInfo)
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterInstance(Config)
                .AsImplementedInterfaces().SingleInstance();

            // Add diagnostics based on configuration
            builder.AddDiagnostics(Config);

            // CORS setup
            builder.RegisterType<CorsSetup>()
                .AsImplementedInterfaces().SingleInstance();

            // Register http client module
            builder.RegisterModule<HttpClientModule>();

            builder.RegisterType<HttpBearerAuthentication>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<PassThroughTokenProvider>()
                .AsImplementedInterfaces().SingleInstance();

            // TODO: Use job database service api
            builder.RegisterType<JobDatabase>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<WorkerDatabase>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<DefaultJobOrchestrator>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<DefaultDemandMatcher>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<CosmosDbServiceClient>()
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<IdentityTokenValidator>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<IoTHubServiceHttpClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<TwinIdentityTokenStore>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<JobOrchestratorEndpointSync>()
                .AsImplementedInterfaces().SingleInstance();

            // Activate all hosts
            builder.RegisterType<HostAutoStart>()
                .AutoActivate()
                .AsImplementedInterfaces().SingleInstance();
        }
    }
}