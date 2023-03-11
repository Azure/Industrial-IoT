// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module
{
    using Autofac;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using System;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Builder;
    using Autofac.Extensions.DependencyInjection;
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;

    /// <summary>
    /// Webservice startup
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Configuration - Initialized in constructor
        /// </summary>
        public IConfigurationRoot Config { get; }

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
            Config = new ConfigurationBuilder()
                .AddConfiguration(configuration)
                .AddFromDotEnvFile()
                .AddEnvironmentVariables()
                .AddEnvironmentVariables(EnvironmentVariableTarget.User)
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
            services.AddRouting();
            services.AddHealthChecks();

            services.AddHttpClient();
            //  services.AddPrometheus();
            //  services.AddOpenTelemetry("OpcPublisher");
            services.AddHostedService<ModuleProcess>();
        }

        /// <summary>
        /// This method is called by the runtime, after the ConfigureServices
        /// method above and used to add middleware
        /// </summary>
        /// <param name="app"></param>
        /// <param name="appLifetime"></param>
        public void Configure(IApplicationBuilder app, IHostApplicationLifetime appLifetime)
        {
            var applicationContainer = app.ApplicationServices.GetAutofacRoot();

            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapHealthChecks("/healthz"));
            //app.UsePrometheus();

            // If you want to dispose of resources that have been resolved in the
            // application container, register for the "ApplicationStopped" event.
            appLifetime.ApplicationStopped.Register(applicationContainer.Dispose);
        }

        /// <summary>
        /// Autofac configuration.
        /// </summary>
        /// <param name="builder"></param>
        public virtual void ConfigureContainer(ContainerBuilder builder)
        {
            // Register configuration interfaces
            builder.RegisterInstance(Config)
                .AsImplementedInterfaces();

            // Register publisher services
            builder.AddPublisherServices();
            builder.RegisterType<StackLogger>()
                .AsImplementedInterfaces().SingleInstance().AutoActivate();

            // Register connectivity services
            builder.AddIoTEdgeServices();
        }
    }
}
