// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault
{
    using Autofac;
    using Autofac.Extensions.DependencyInjection;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Swagger;
    using Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1;
    using Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Auth;
    using Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Filters;
    using Microsoft.Azure.IIoT.Services;
    using Microsoft.Azure.IIoT.Services.Auth;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.Services.AppAuthentication;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using Serilog;
    using Swashbuckle.AspNetCore.Swagger;
    using System;
    using CorsSetup = IIoT.Services.Cors.CorsSetup;

    /// <summary>
    /// Webservice startup
    /// </summary>
    public class Startup
    {

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
        public Startup(IHostingEnvironment env)
        {
            Environment = env;

            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, true)
                .AddEnvironmentVariables();

            IConfigurationRoot config;
            try
            {
                var builtConfig = configBuilder.Build();
                var keyVault = builtConfig["KeyVault"];
                if (!String.IsNullOrWhiteSpace(keyVault))
                {
                    var appSecret = builtConfig["Auth:AppSecret"];
                    if (String.IsNullOrWhiteSpace(appSecret))
                    {
                        // try managed service identity
                        var azureServiceTokenProvider = new AzureServiceTokenProvider();
                        var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
                        configBuilder.AddAzureKeyVault(
                            keyVault,
                            keyVaultClient,
                            new PrefixKeyVaultSecretManager("Service")
                            );
                    }
                    else
                    {
                        // use AzureAD token
                        configBuilder.AddAzureKeyVault(
                            keyVault,
                            builtConfig["Auth:AppId"],
                            appSecret,
                            new PrefixKeyVaultSecretManager("Service")
                            );
                    }
                }
            }
            catch
            {
            }
            config = configBuilder.Build();
            Config = new Config(config);
        }

        /// <summary>
        /// This is where you register dependencies, add services to the
        /// container. This method is called by the runtime, before the
        /// Configure method below.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            // Setup (not enabling yet) CORS
            services.AddCors();

            // Add authentication
            services.AddJwtBearerAuthentication(Config, Environment.IsDevelopment());

            // Add authorization
            services.AddAuthorization(options =>
            {
                options.AddV1Policies(Config, Config.ServicesConfig);
            });

            // Add controllers as services so they'll be resolved.
            services.AddMvc(options =>
                options.Filters.Add(typeof(ExceptionsFilterAttribute))
                )
                .AddControllersAsServices()
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.Formatting = Formatting.Indented;
                    options.SerializerSettings.Converters.Add(new ExceptionConverter(
                        Environment.IsDevelopment()));
                    options.SerializerSettings.MaxDepth = 10;
                });

            services.AddApplicationInsightsTelemetry(Config.Configuration);

            services.AddSwaggerEx(Config, new Info
            {
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
        /// <param name="logger"></param>
        /// <param name="appLifetime"></param>
        public void Configure(
            IApplicationBuilder app,
            IHostingEnvironment env,
            ILogger logger,
            IApplicationLifetime appLifetime)
        {
            if (Config.AuthRequired)
            {
                app.UseAuthentication();
            }

            app.EnableCors();

            app.UseSwaggerEx(Config, new Info
            {
                Title = ServiceInfo.NAME,
                Version = VersionInfo.PATH,
                Description = ServiceInfo.DESCRIPTION,
            });

            app.UseMvc();

            // If you want to dispose of resources that have been resolved in the
            // application container, register for the "ApplicationStopped" event.
            appLifetime.ApplicationStopped.Register(ApplicationContainer.Dispose);

            // Print some useful information at bootstrap time
            logger.Information($"{ServiceInfo.NAME} web service started",
                new { Uptime.ProcessId, env });
        }

        /// <summary>
        /// Autofac configuration. Find more information here:
        /// @see http://docs.autofac.org/en/latest/integration/aspnetcore.html
        /// </summary>
        public IContainer ConfigureContainer(IServiceCollection services)
        {
            ContainerBuilder builder = new ContainerBuilder();

            // Populate from services di
            builder.Populate(services);

            // By default Autofac uses a request lifetime, creating new objects
            // for each request, which is good to reduce the risk of memory
            // leaks, but not so good for the overall performance.

            // Register configuration interfaces
            builder.RegisterInstance(Config)
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterInstance(Config.ServicesConfig)
                .AsImplementedInterfaces().SingleInstance();

            // register the serilog logger
            builder.RegisterInstance(Log.Logger).As<ILogger>();

            // CORS setup
            builder.RegisterType<CorsSetup>()
                .AsImplementedInterfaces().SingleInstance();

            // Register endpoint services and ...
            builder.RegisterType<KeyVaultCertificateGroup>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<CosmosDBApplicationsDatabase>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<CosmosDBCertificateRequest>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<OpcVaultDocumentDbRepository>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<WarmStartDatabase>()
                .AsImplementedInterfaces().SingleInstance();

            return builder.Build();
        }
    }
}
