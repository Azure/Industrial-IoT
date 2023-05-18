// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module
{
    using Azure.IIoT.OpcUa.Publisher.Module.Runtime;
    using Autofac;
    using Autofac.Extensions.DependencyInjection;
    using Furly;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.OpenApi.Models;
    using OpenTelemetry.Metrics;
    using OpenTelemetry.Resources;
    using System;

    /// <summary>
    /// Webservice startup
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Configuration
        /// </summary>
        public IConfigurationRoot Configuration { get; }

        /// <summary>
        /// Current hosting environment
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
            Configuration = new ConfigurationBuilder()
                .AddConfiguration(configuration)
                .AddFromDotEnvFile()
                .AddEnvironmentVariables()
                .Build();
        }

        /// <summary>
        /// Configure services
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(options => options
                .AddFilter(typeof(IAwaitable).Namespace, LogLevel.Warning)
                .AddSimpleConsole(options =>
                {
                    // options.SingleLine = true;
                    options.IncludeScopes = true;
                    options.UseUtcTimestamp = true;
                    options.TimestampFormat = "[HH:mm:ss.ffff] ";
                })
                .AddDebug())
                ;

            services.AddHttpClient();

            services.AddRouting();
            services.AddHealthChecks();
            services.AddMemoryCache();

            services.AddAuthorization();
            services.AddAuthentication()
                .UsingConfiguredApiKey()
                ;

            services.AddOpenTelemetry()
                .ConfigureResource(r => r
                    .AddService(Constants.EntityTypePublisher,
                        default, GetType().Assembly.GetReleaseVersion().ToString()))
                .WithMetrics(builder => builder
                    .AddMeter(Diagnostics.Meter.Name)
                    .AddRuntimeInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddPrometheusExporter())
                ;

            services.AddControllers()
                .AddJsonSerializer()
                .AddMessagePackSerializer()
                ;

            services.AddSwagger(Constants.EntityTypePublisher, string.Empty);
        }

        /// <summary>
        /// This method is called by the runtime, after the ConfigureServices
        /// method above and used to add middleware
        /// </summary>
        /// <param name="app"></param>
        /// <param name="appLifetime"></param>
        public void Configure(IApplicationBuilder app, IHostApplicationLifetime appLifetime)
        {
            app.UseRouting();

            // app.UseHsts();
            // app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

#if DEBUG
            app.UseSwagger();
#endif
            app.UseOpenTelemetryPrometheusScrapingEndpoint();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/healthz");
            });

            var applicationContainer = app.ApplicationServices.GetAutofacRoot();
            appLifetime.ApplicationStopped.Register(applicationContainer.Dispose);
        }

        /// <summary>
        /// Autofac configuration.
        /// </summary>
        /// <param name="builder"></param>
        public virtual void ConfigureContainer(ContainerBuilder builder)
        {
            // Register publisher services
            builder.AddPublisherServices();

            // Register transport services
            builder.AddMqttClient(Configuration);
            builder.AddIoTEdgeServices(Configuration);

            // Register configuration interfaces
            builder.RegisterInstance(Configuration)
                .AsImplementedInterfaces();
        }
    }
}
