// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module
{
    using Azure.IIoT.OpcUa.Publisher.Module.Runtime;
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Core.Serialization;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Console;
    using Microsoft.Extensions.Options;
#if IIOT_MCP
    using Opc.Ua.Mcp;
#endif
    using OpenTelemetry.Logs;
    using OpenTelemetry.Metrics;
    using OpenTelemetry.Resources;
    using OpenTelemetry.Trace;
    using System;
    using System.Diagnostics.CodeAnalysis;

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
        /// Whether the OPC UA MCP tool server is enabled.
        /// </summary>
        private bool McpServerEnabled => Configuration
            .GetValue<bool>(PublisherConfig.EnableMcpServerKey);

        /// <summary>
        /// Configure services
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        [UnconditionalSuppressMessage("Trimming", "IL2026",
            Justification = "AddConsoleFormatter<Syslog, ConsoleFormatterOptions> binds " +
            "the framework ConsoleFormatterOptions whose members are statically " +
            "analyzable; the trimming warning is a framework false positive.")]
        [UnconditionalSuppressMessage("AOT", "IL3050",
            Justification = "AddConsoleFormatter<Syslog, ConsoleFormatterOptions> binds " +
            "the framework ConsoleFormatterOptions whose members are statically " +
            "analyzable; the AOT warning is a framework false positive.")]
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(options => options
                .AddDebug()
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
                }))
                ;

            services.AddHttpClient();
            services.AddResourceMonitoring(Configuration);
            services.AddExceptionSummarizer(builder =>
            {
                // --mcp brings in AddStandardResilienceHandler, which registers
                // Microsoft's HttpExceptionSummaryProvider. Two providers may
                // not claim the same exception type, so the overlapping types
                // are ceded to it - but only then, because on the default path
                // nothing else would describe them.
                builder.AddDefaultProviders(httpProviderRegistered: McpServerEnabled);
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
                .WithTracing(Configuration, builder => builder
                    .SetSampler(new AlwaysOnSampler())
                    .AddSource(Diagnostics.Namespace)
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation())
                .WithMetrics(Configuration, builder => builder
                    .AddMeter(Diagnostics.Meter.Name))
                ;

            services.ConfigureHttpJsonOptions(options => Json.ApplyTo(options.SerializerOptions));

            // The REST surface serves its OpenAPI document through the built-in
            // Microsoft.AspNetCore.OpenApi generator, which is source generator /
            // trim friendly (unlike the removed Swashbuckle pipeline). The legacy
            // "useopenapiv3" command line flag is still honored: when set the
            // newer OpenAPI 3.1 document is emitted, otherwise the default OpenAPI
            // 3.0 document. The removed Swashbuckle pipeline previously toggled a
            // Swagger 2.0 document which the built-in generator does not produce;
            // the flag therefore only selects between OpenAPI 3.x document versions
            // now (this affects the generated document only, not the REST API).
            var useOpenApiV3 = Configuration.GetValue<bool>(
                Runtime.Configuration.OpenApi.UseOpenApiV3Key);
            services.AddOpenApi(options => options.OpenApiVersion = useOpenApiV3
                ? Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_1
                : Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0);

            // Register configuration interfaces
            services.AddSingleton<IConfiguration>(Configuration);
            services.AddSingleton<IConfigurationRoot>(Configuration);

            // The OPC UA MCP tool server rides the module's existing http
            // listeners and authentication; see Configure below for the mapping.
            if (McpServerEnabled)
            {
#if IIOT_MCP
                services.AddOpcUaMcpCore();
                services.AddOpcUaMcpDiagnostics(options =>
                    options.EnableDiagnosticsTools = true);
                services.AddMcpServer()
                    .WithHttpTransport()
                    .WithOpcUaMcpFilters()
                    .WithOpcUaCoreTools(McpToolProfile.Full)
                    .WithOpcUaDiagnosticsTools(McpToolProfile.Full,
                        diagnosticsToolsEnabled: true);
#else
                // Compiled out of the ahead-of-time published module, where the
                // MCP SDK's reflective schema generation cannot work. Fail loudly
                // rather than starting without the endpoint that was asked for.
                throw new NotSupportedException(
                    "The MCP tool server is not available in an ahead-of-time " +
                    "published OPC Publisher. Remove --mcp, or use a module that " +
                    "was not published with IIoTPublishAot.");
#endif
            }

            // Register publisher services and transports (previously registered
            // through Autofac's ConfigureContainer). This is a separate overridable
            // method so hosts (e.g. tests) can suppress the production transport
            // wiring, mirroring the previously empty ConfigureContainer override.
            ConfigurePublisherServices(services);
        }

        /// <summary>
        /// Configure publisher services and connectivity transports. Overridable so
        /// test hosts can substitute mock transports.
        /// </summary>
        /// <param name="services"></param>
        protected virtual void ConfigurePublisherServices(IServiceCollection services)
        {
            services.AddPublisherServices();

            //
            // Order is important here because we want
            // to fall back in the reverse order for
            // sending operational and discovery events!
            //
            CoreServiceCollectionEx.AddMemoryKeyValueStore(services);
            services.AddDaprStateStoreClient(Configuration);
            CoreServiceCollectionEx.AddNullEventClient(services);
            services.AddFileSystemEventClient(Configuration);
            services.AddFileSystemRpcServer(Configuration);
            services.AddHttpEventClient(Configuration);
            services.AddDaprPubSubClient(Configuration);
            services.AddEventHubsClient(Configuration);
            services.AddMqttClient(Configuration);
            services.AddIoTEdgeServices(Configuration);
            services.AddIoTOperationsServices(Configuration);
        }

        /// <summary>
        /// This method is called by the runtime, after the ConfigureServices
        /// method above and used to add middleware
        /// </summary>
        /// <param name="app"></param>
        /// <param name="appLifetime"></param>
#pragma warning disable CA1822 // Mark members as static
        public void Configure(IApplicationBuilder app)
#pragma warning restore CA1822 // Mark members as static
        {
            // Surface direct method call failures (which bypass the ASP.NET
            // request pipeline) into the module logs and support bundles.
            Filters.RouterExceptionFilterAttribute.SetLogger(
                app.ApplicationServices.GetRequiredService<ILoggerFactory>());

            app.UseRouting();

            // app.UseHsts();
            // app.UseHttpsRedirection();
            app.UseResponseCompression();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseOpenTelemetryPrometheusEndpoint();

            // OpenAPI document endpoint is exposed unless disabled via the
            // "disableopenapi" command line flag (PublisherOptions).
            var openApiEnabled = app.ApplicationServices
                .GetService<IOptions<PublisherOptions>>()?.Value
                .DisableOpenApiEndpoint != true;

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapPublisherApi();
                if (openApiEnabled)
                {
                    endpoints.MapOpenApi();
                }
                endpoints.MapHealthChecks("/healthz");

                // Mapped inside the same pipeline as the REST api, and after
                // UseAuthentication/UseAuthorization above, so the MCP endpoint
                // is reached over the already configured listeners and is
                // subject to the same api key authentication. RequireAuthorization
                // makes that explicit rather than inherited by accident.
                var mcpEnabled = app.ApplicationServices
                    .GetService<IOptions<PublisherOptions>>()?.Value
                    .EnableMcpServer == true;
                if (mcpEnabled)
                {
#if IIOT_MCP
                    endpoints.MapMcp("/mcp").RequireAuthorization();
#endif
                }
            });
        }
    }
}
