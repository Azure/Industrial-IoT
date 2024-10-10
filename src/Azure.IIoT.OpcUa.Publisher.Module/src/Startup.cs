// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module
{
    using Azure.IIoT.OpcUa.Publisher.Module.Runtime;
    using Autofac;
    using Autofac.Extensions.DependencyInjection;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Console;
    using OpenTelemetry.Logs;
    using OpenTelemetry.Metrics;
    using OpenTelemetry.Resources;
    using OpenTelemetry.Trace;
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
        /// Create startup
        /// </summary>
        /// <param name="configuration"></param>
        public Startup(IConfiguration configuration)
        {
            Configuration = new ConfigurationBuilder()
                .AddConfiguration(configuration)
                .AddFromDotEnvFile()
                .AddEnvironmentVariables()
                .Build();

            // Set polling mode on file watcher if configured
            if (Configuration.GetValue<string>(PublisherConfig.UseFileChangePollingKey)?
                .Equals("True", StringComparison.OrdinalIgnoreCase) ?? false)
            {
                Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "1");
            }
        }

        /// <summary>
        /// Configure services
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(options => options
                .AddConsole()
                .AddConsoleFormatter<Syslog, ConsoleFormatterOptions>()
                .AddOpenTelemetry(Configuration, options =>
                {
                    options.IncludeScopes = true;
                    options.ParseStateValues = true;
                    options.IncludeFormattedMessage = true;
                    options.SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddTelemetrySdk()
                        .AddService(Constants.EntityTypePublisher,
                            default, GetType().Assembly.GetReleaseVersion().ToString()));
                    options.AddOtlpExporter(Configuration);
                })
                .AddDebug())
                ;

            services.AddHttpClient();
            services.AddResourceMonitoring(Configuration);
            services.AddExceptionSummarizer(builder =>
            {
                builder.AddDefaultProviders();
                // TODO: Add opc ua exceptions
            });

            services.AddRouting();
            services.AddHealthChecks();
            services.AddMemoryCache();
            services.AddResponseCompression(options => options.EnableForHttps = true);

            services.AddAuthorization();
            services.AddAuthentication()
                .UsingConfiguredApiKey()
                ;

            services.AddOpenTelemetry()
                .ConfigureResource(r => r
                    .AddService(Constants.EntityTypePublisher,
                        default, GetType().Assembly.GetReleaseVersion().ToString()))
                .WithTracing(builder => builder
                    .SetSampler(new AlwaysOnSampler())
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddOtlpExporter(Configuration))
                .WithMetrics(builder => builder
                    .AddMeter(Diagnostics.Meter.Name)
                    .AddRuntimeInstrumentation(Configuration)
                    .AddPrometheusExporter(Configuration)
                    .AddOtlpExporter(Configuration))
                ;

            services.AddControllers()
                .AddJsonSerializer()
                .AddMessagePackSerializer()
                ;

            services.AddOpenApi();
        }

        /// <summary>
        /// This method is called by the runtime, after the ConfigureServices
        /// method above and used to add middleware
        /// </summary>
        /// <param name="app"></param>
        /// <param name="appLifetime"></param>
#pragma warning disable CA1822 // Mark members as static
        public void Configure(IApplicationBuilder app, IHostApplicationLifetime appLifetime)
#pragma warning restore CA1822 // Mark members as static
        {
            app.UseRouting();

            // app.UseHsts();
            // app.UseHttpsRedirection();
            app.UseResponseCompression();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseOpenApi();
            app.UseOpenTelemetryPrometheusEndpoint();

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
            // Register publisher services and transports
            builder.AddPublisherServices();

            //
            // Order is important here because we want
            // to fall back in the reverse order for
            // sending operational and discovery events!
            //
            builder.AddMemoryKeyValueStore();
            builder.AddDaprStateStoreClient(Configuration);

            builder.AddNullEventClient();
            builder.AddFileSystemEventClient(Configuration);
            builder.AddFileSystemRpcServer(Configuration);
            builder.AddHttpEventClient(Configuration);
            builder.AddDaprPubSubClient(Configuration);
            builder.AddEventHubsClient(Configuration);
            builder.AddMqttClient(Configuration);
            builder.AddIoTEdgeServices(Configuration);

            // Register configuration interfaces
            builder.RegisterInstance(Configuration)
                .AsImplementedInterfaces();
        }
    }
}
