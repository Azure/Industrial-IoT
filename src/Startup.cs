// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Twin {
    using Microsoft.Azure.IIoT.OpcUa.Services.Twin.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Services;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Http.Auth;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Http.Ssl;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Services;
    using Microsoft.Azure.IIoT.Services.Auth;
    using Microsoft.Azure.IIoT.Services.Auth.Azure;
    using Microsoft.Azure.IIoT.Services.Cors;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Autofac;
    using Autofac.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using Swashbuckle.AspNetCore.Swagger;
    using System;
    using ILogger = Diagnostics.ILogger;

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
        /// Di container - Initialized in `ConfigureServices`
        /// </summary>
        public IContainer ApplicationContainer { get; private set; }

        /// <summary>
        /// Created through builder
        /// </summary>
        /// <param name="env"></param>
        public Startup(IHostingEnvironment env) {
            Environment = env;

            var config = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile(
                    "appsettings.json", true, true)
                .AddJsonFile(
                    $"appsettings.{env.EnvironmentName}.json", true, true)
                .AddEnvironmentVariables()
                .Build();

            Config = new Config(config);
        }

        /// <summary>
        /// This is where you register dependencies, add services to the
        /// container. This method is called by the runtime, before the
        /// Configure method below.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public IServiceProvider ConfigureServices(IServiceCollection services) {

            // Setup (not enabling yet) CORS
            services.AddCors();

            // Add authentication
            services.AddJwtBearerAuthentication(Config, Config.ClientId,
                Environment.IsDevelopment());

            // Add authorization
            services.AddAuthorization(options => {
                options.AddV1Policies(Config);
            });

            // TODO: Remove http client factory and use
            // services.AddHttpClient();

            // Add controllers as services so they'll be resolved.
            services.AddMvc().AddControllersAsServices().AddJsonOptions(options => {
                options.SerializerSettings.Formatting = Formatting.Indented;
                options.SerializerSettings.Converters.Add(new ExceptionConverter(
                    Environment.IsDevelopment()));
                options.SerializerSettings.MaxDepth = 10;
            });

            services.AddSwagger(Config, new Info {
                Title = ServiceInfo.NAME,
                Version = VersionInfo.PATH,
                Description = ServiceInfo.DESCRIPTION,
            });

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
        /// <param name="env"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="appLifetime"></param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env,
            ILoggerFactory loggerFactory, IApplicationLifetime appLifetime) {

            var log = ApplicationContainer.Resolve<ILogger>();
            loggerFactory.AddConsole(Config.Configuration.GetSection("Logging"));

            if (Config.AuthRequired) {
                app.UseAuthentication();
            }

            app.EnableCors();

            app.UseSwagger(Config, new Info {
                Title = ServiceInfo.NAME,
                Version = VersionInfo.PATH,
                Description = ServiceInfo.DESCRIPTION,
            });

            app.UseMvc();

            // If you want to dispose of resources that have been resolved in the
            // application container, register for the "ApplicationStopped" event.
            appLifetime.ApplicationStopped.Register(ApplicationContainer.Dispose);

            // Print some useful information at bootstrap time
            log.Info($"{ServiceInfo.NAME} web service started",
                () => new { Uptime.ProcessId, env });
        }

        /// <summary>
        /// Autofac configuration. Find more information here:
        /// see http://docs.autofac.org/en/latest/integration/aspnetcore.html
        /// </summary>
        public IContainer ConfigureContainer(IServiceCollection services) {
            var builder = new ContainerBuilder();

            // Populate from services di
            builder.Populate(services);

            // Register logger
            builder.RegisterInstance(Config.Logger)
                .AsImplementedInterfaces().SingleInstance();
            // Register configuration interfaces
            builder.RegisterInstance(Config)
                .AsImplementedInterfaces().SingleInstance();
            // CORS setup
            builder.RegisterType<CorsSetup>()
                .AsImplementedInterfaces().SingleInstance();

            // Http client services ...
            builder.RegisterType<HttpClient>().SingleInstance()
                .AsImplementedInterfaces();
            builder.RegisterType<HttpHandlerFactory>().SingleInstance()
                .AsImplementedInterfaces();
            builder.RegisterType<HttpClientFactory>().SingleInstance()
                .AsImplementedInterfaces();

            // ... with bearer auth
            if (Config.AuthRequired) {
                builder.RegisterType<BehalfOfTokenProvider>()
                    .AsImplementedInterfaces().SingleInstance();
                builder.RegisterType<DistributedTokenCache>()
                   .AsImplementedInterfaces().SingleInstance();
                builder.RegisterType<HttpBearerAuthentication>()
                    .AsImplementedInterfaces().SingleInstance();
            }
#if DEBUG
            builder.RegisterType<NoOpCertValidator>()
                .AsImplementedInterfaces();
#endif

            // Iot hub services
            builder.RegisterType<IoTHubServiceHttpClient>()
                .AsImplementedInterfaces().SingleInstance();

            // Opc ua services
            builder.RegisterType<RegistryServices>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<TwinClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<SupervisorClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<DiscoveryClient>()
                .AsImplementedInterfaces().SingleInstance();

            return builder.Build();
        }
    }
}
