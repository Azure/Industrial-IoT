// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Hub.Edgemanager {
    using Microsoft.Azure.IIoT.Services.Common.Hub.Edgemanager.Runtime;
    using Microsoft.Azure.IIoT.AspNetCore.Correlation;
    using Microsoft.Azure.IIoT.AspNetCore.Cors;
    using Microsoft.Azure.IIoT.AspNetCore.Auth;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Deploy;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Deploy;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Deploy;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Services;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.OpenApi.Models;
    using Microsoft.Extensions.Hosting;
    using Autofac;
    using Autofac.Extensions.DependencyInjection;
    using System;
    using Newtonsoft.Json;
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
            services.AddControllers()
                .AddNewtonsoftJson(options => {
                    options.SerializerSettings.Formatting = Formatting.Indented;
                    options.SerializerSettings.Converters.Add(new ExceptionConverter(
                        Environment.IsDevelopment()));
                    options.SerializerSettings.MaxDepth = 10;
                });
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

            builder.RegisterInstance(Config)
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterInstance(Config.Configuration)
                .AsImplementedInterfaces().SingleInstance();

            // Register logger
            builder.AddDiagnostics(Config);

            // Register metrics logger
            builder.RegisterType<MetricsLogger>()
                .AsImplementedInterfaces().SingleInstance();

            // CORS setup
            builder.RegisterType<CorsSetup>()
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<IoTHubServiceClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<IoTHubEdgeBaseDeployment>()
                .AsImplementedInterfaces().SingleInstance();

            // TODO - use service client everywhere and move to publisher and twin service
            // TODO - then remove references
            builder.RegisterType<IoTHubPublisherDeployment>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<IoTHubSupervisorDeployment>()
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