// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Runtime
{
    using Autofac;
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Publisher.Module.Controllers;
    using Furly.Azure.EventHubs;
    using Furly.Azure.IoT.Edge;
    using Furly.Extensions.AspNetCore.OpenApi;
    using Furly.Extensions.Configuration;
    using Furly.Extensions.Dapr;
    using Furly.Extensions.Logging;
    using Furly.Extensions.Messaging.Runtime;
    using Furly.Extensions.Mqtt;
    using Furly.Extensions.Rpc.Runtime;
    using Furly.Tunnel.Router.Services;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Server.Kestrel.Core;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Console;
    using Microsoft.Extensions.Options;
    using Microsoft.OpenApi.Models;
    using OpenTelemetry.Exporter;
    using OpenTelemetry.Logs;
    using OpenTelemetry.Metrics;
    using OpenTelemetry.Trace;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Configuration extensions
    /// </summary>
    public static class Configuration
    {
        /// <summary>
        /// Add all publisher dependencies minus connectivity components.
        /// </summary>
        /// <param name="builder"></param>
        public static void AddPublisherServices(this ContainerBuilder builder)
        {
            builder.AddDefaultJsonSerializer();
            builder.AddNewtonsoftJsonSerializer();
            builder.AddMessagePackSerializer();
            builder.AddPublisherCore();

            builder.RegisterType<HealthCheckRegistrar>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<CommandLine>()
                .AsImplementedInterfaces().AsSelf().SingleInstance();
            builder.RegisterType<LoggingLevel>()
                .AsImplementedInterfaces();
            builder.RegisterType<ConsoleLogging<ConsoleFormatterOptions>>()
                .AsImplementedInterfaces();
            builder.RegisterType<ConsoleLogging<SimpleConsoleFormatterOptions>>()
                .AsImplementedInterfaces();
            builder.RegisterType<ConsoleLogging<JsonConsoleFormatterOptions>>()
                .AsImplementedInterfaces();
            builder.RegisterType<Syslog>()
                .AsImplementedInterfaces().AsSelf().SingleInstance();
            builder.RegisterType<Kestrel>()
                .AsImplementedInterfaces();

            // Register and configure controllers
            builder.RegisterType<MethodRouter>()
                .AsImplementedInterfaces().SingleInstance()
                .PropertiesAutowired(
                    PropertyWiringOptions.AllowCircularDependencies);
            builder.RegisterType<Router>()
                .AsImplementedInterfaces();

            builder.RegisterType<PublisherController>()
                .AsImplementedInterfaces();
            builder.RegisterType<ConfigurationController>()
                .AsImplementedInterfaces();
            builder.RegisterType<WriterController>()
                .AsImplementedInterfaces();
            builder.RegisterType<GeneralController>()
                .AsImplementedInterfaces();
            builder.RegisterType<HistoryController>()
                .AsImplementedInterfaces();
            builder.RegisterType<DiscoveryController>()
                .AsImplementedInterfaces();
            builder.RegisterType<CertificatesController>()
                .AsImplementedInterfaces();
            builder.RegisterType<DiagnosticsController>()
                .AsImplementedInterfaces();
        }

        /// <summary>
        /// Add resource monitoring
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        public static void AddResourceMonitoring(this IServiceCollection services,
            IConfiguration configuration)
        {
            var publisherOptions = new PublisherConfig(configuration).ToOptions();
            if (publisherOptions.Value.DisableResourceMonitoring != true)
            {
                services.AddResourceMonitoring();
            }
        }

        /// <summary>
        /// Add mqtt client
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        public static void AddMqttClient(this ContainerBuilder builder,
            IConfiguration configuration)
        {
            var mqttOptions = new MqttOptions();
            new MqttBroker(configuration).Configure(mqttOptions);
            if (mqttOptions.HostName != null)
            {
                builder.AddMqttClient();
                builder.RegisterType<MqttBroker>()
                    .AsImplementedInterfaces();
                builder.RegisterType<SchemaTopicBuilder>()
                    .AsImplementedInterfaces();
            }
        }

        /// <summary>
        /// Add IoT edge services
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        public static void AddIoTEdgeServices(this ContainerBuilder builder,
            IConfiguration configuration)
        {
            // Validate edge configuration
            var iotEdgeOptions = new IoTEdgeClientOptions();
            new IoTEdge(configuration).Configure(iotEdgeOptions);
            if (iotEdgeOptions.EdgeHubConnectionString != null)
            {
                builder.AddIoTEdgeServices();
                builder.RegisterType<IoTEdge>()
                    .AsImplementedInterfaces();
            }
        }

        /// <summary>
        /// Add Event Hubs client
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        public static void AddEventHubsClient(this ContainerBuilder builder,
            IConfiguration configuration)
        {
            // Validate edge configuration
            var eventHubsOptions = new EventHubsClientOptions();
            new EventHubs(configuration).Configure(eventHubsOptions);
            if (eventHubsOptions.ConnectionString != null)
            {
                builder.AddHubEventClient();
                builder.RegisterType<EventHubs>()
                    .AsImplementedInterfaces();
            }
        }

        /// <summary>
        /// Add file system client
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        public static void AddFileSystemEventClient(this ContainerBuilder builder,
            IConfiguration configuration)
        {
            var fsOptions = new FileSystemEventClientOptions();
            new FileSystem(configuration).Configure(fsOptions);
            if (fsOptions.OutputFolder != null)
            {
                builder.AddFileSystemEventClient();
                builder.RegisterType<FileSystem>()
                    .AsImplementedInterfaces();
                builder.RegisterType<AvroWriter>()
                    .AsImplementedInterfaces();
                builder.RegisterType<ConsoleWriter>()
                    .AsImplementedInterfaces();
            }
        }

        /// <summary>
        /// Add file system rpc server
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        public static void AddFileSystemRpcServer(this ContainerBuilder builder,
            IConfiguration configuration)
        {
            var fsOptions = new FileSystemRpcServerOptions();
            new FileSystem(configuration).Configure(fsOptions);
            if (fsOptions.RequestFilePath != null)
            {
                builder.AddFileSystemRpcServer();
                builder.RegisterType<FileSystem>()
                    .AsImplementedInterfaces();
            }
        }

        /// <summary>
        /// Add http event client
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        public static void AddHttpEventClient(this ContainerBuilder builder,
            IConfiguration configuration)
        {
            var httpOptions = new HttpEventClientOptions();
            new Http(configuration).Configure(httpOptions);
            if (httpOptions.HostName != null)
            {
                builder.AddHttpEventClient();
                builder.RegisterType<Http>()
                    .AsImplementedInterfaces();
            }
        }

        /// <summary>
        /// Add dapr client
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        public static void AddDaprPubSubClient(this ContainerBuilder builder,
            IConfiguration configuration)
        {
            var daprOptions = new DaprOptions();
            new Dapr(configuration).Configure(daprOptions);
            if (!string.IsNullOrWhiteSpace(daprOptions.PubSubComponent))
            {
                builder.AddDaprPubSubClient();
                builder.RegisterType<Dapr>()
                    .AsImplementedInterfaces();
            }
        }

        /// <summary>
        /// Add dapr client
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        public static void AddDaprStateStoreClient(this ContainerBuilder builder,
            IConfiguration configuration)
        {
            var daprOptions = new DaprOptions();
            new Dapr(configuration).Configure(daprOptions);
            if (!string.IsNullOrWhiteSpace(daprOptions.StateStoreName))
            {
                builder.AddDaprStateStoreClient();
                builder.RegisterType<Dapr>()
                    .AsImplementedInterfaces();
            }
        }

        /// <summary>
        /// Add runtime instrumentation if requested
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static MeterProviderBuilder AddRuntimeInstrumentation(this MeterProviderBuilder builder,
            IConfiguration configuration)
        {
            var options = new Otlp(configuration);
            if (options.AddRuntimeInstrumentation)
            {
                builder
                    .AddRuntimeInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation();
            }
            return builder;
        }

        /// <summary>
        /// Add otlp exporter if configured
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        public static MeterProviderBuilder AddOtlpExporter(this MeterProviderBuilder builder,
            IConfiguration configuration)
        {
            var option = new Otlp(configuration);
            var exporterOptions = new OtlpExporterOptions();
            option.Configure(exporterOptions);
            if (exporterOptions.Endpoint.Host != Otlp.OtlpEndpointDisabled)
            {
                builder.SetMaxMetricStreams(option.MaxMetricStreams);
                builder.ConfigureServices(services => services.ConfigureOtlpExporter());
                builder.AddOtlpExporter();
            }
            return builder;
        }

        /// <summary>
        /// Add prometheus exporter if configured
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        public static MeterProviderBuilder AddPrometheusExporter(this MeterProviderBuilder builder,
            IConfiguration configuration)
        {
            var option = new Otlp(configuration);
            if (option.AddPrometheusEndpoint)
            {
                builder.ConfigureServices(services => services.AddSingleton<Otlp>());
                builder.SetMaxMetricStreams(option.MaxMetricStreams);

                builder.AddPrometheusExporter(options =>
                {
                    options.DisableTotalNameSuffixForCounters =
                        !option.EnableTotalNameSuffixForCounters;

                    //
                    // Configures scrape endpoint response caching. Multiple scrape requests
                    // within the cache duration time period will receive the same previously
                    // generated response. Disable response caching. The default value is 300.
                    //
                    options.ScrapeResponseCacheDurationMilliseconds = 0;
                });
            }
            return builder;
        }

        /// <summary>
        /// Use prometheus endpoint
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseOpenTelemetryPrometheusEndpoint(this IApplicationBuilder app)
        {
            if (app.ApplicationServices.GetService<Otlp>()?.AddPrometheusEndpoint == true)
            {
                app.UseOpenTelemetryPrometheusScrapingEndpoint();
            }
            return app;
        }

        /// <summary>
        /// Add open api
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection AddOpenApi(this IServiceCollection services)
        {
            return services
                .AddSingleton<IConfigureOptions<OpenApiOptions>, OpenApi>()
                .AddSingleton<IConfigureNamedOptions<OpenApiOptions>, OpenApi>()
                .AddSwagger(Constants.EntityTypePublisher, string.Empty);
        }

        /// <summary>
        /// Use open api
        /// </summary>
        /// <param name="builder"></param>
        public static IApplicationBuilder UseOpenApi(this IApplicationBuilder builder)
        {
            var options = builder.ApplicationServices.GetService<IOptions<PublisherOptions>>();
            if (options?.Value.DisableOpenApiEndpoint != true)
            {
                return builder.UseSwagger();
            }
            return builder;
        }

        /// <summary>
        /// Add otlp exporter if configured
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        public static OpenTelemetryLoggerOptions AddOtlpExporter(
            this OpenTelemetryLoggerOptions builder, IConfiguration configuration)
        {
            builder.AddOtlpExporter((o, _) => new Otlp(configuration).Configure(o));
            return builder;
        }

        /// <summary>
        /// Add open telemetry to the logging builder
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        /// <param name="configure"></param>
        public static ILoggingBuilder AddOpenTelemetry(this ILoggingBuilder builder,
            IConfiguration configuration, Action<OpenTelemetryLoggerOptions> configure)
        {
            var exporterOptions = new OtlpExporterOptions();
            var configurator = new Otlp(configuration);
            configurator.Configure(exporterOptions);
            if (exporterOptions.Endpoint.Host != Otlp.OtlpEndpointDisabled)
            {
                builder.AddOpenTelemetry(configure);
            }
            return builder;
        }

        /// <summary>
        /// Add otlp exporter if configured
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        public static TracerProviderBuilder AddOtlpExporter(this TracerProviderBuilder builder,
            IConfiguration configuration)
        {
            var exporterOptions = new OtlpExporterOptions();
            new Otlp(configuration).Configure(exporterOptions);
            if (exporterOptions.Endpoint.Host != Otlp.OtlpEndpointDisabled)
            {
                builder.ConfigureServices(services => services.ConfigureOtlpExporter());
                builder.AddOtlpExporter();
            }
            return builder;
        }

        /// <summary>
        /// Configure otlp exporter
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        private static IServiceCollection ConfigureOtlpExporter(this IServiceCollection services)
        {
            return services
                .AddSingleton<IConfigureOptions<OtlpExporterOptions>, Otlp>()
                .AddSingleton<IConfigureNamedOptions<OtlpExporterOptions>, Otlp>()
                .AddSingleton<IConfigureOptions<MetricReaderOptions>, Otlp>()
                .AddSingleton<IConfigureNamedOptions<MetricReaderOptions>, Otlp>();
        }

        /// <summary>
        /// Adds secrets from a env file that is located at $ADDITIONAL_CONFIGURATION
        /// Defaults to .env file in docker /run/secrets folder.
        /// </summary>
        /// <param name="builder"></param>
        public static IConfigurationBuilder AddSecrets(this IConfigurationBuilder builder)
        {
            try
            {
                return builder.Add(new DotEnvFileSource(
                    Environment.GetEnvironmentVariable("ADDITIONAL_CONFIGURATION")
                        ?? "/run/secrets/.env"));
            }
            catch (UnauthorizedAccessException)
            {
                return builder;
            }
        }

        /// <summary>
        /// Otlp configuration from environment
        /// </summary>
        internal sealed class Otlp : ConfigureOptionBase<OtlpExporterOptions>,
            IConfigureOptions<MetricReaderOptions>, IConfigureNamedOptions<MetricReaderOptions>
        {
            public const string EnableMetricsKey = "EnableMetrics";
            public const string OtlpCollectorEndpointKey = "OtlpCollectorEndpoint";
            public const string OtlpMaxMetricStreamsKey = "OtlpMaxMetricStreams";
            public const string OtlpExportIntervalMillisecondsKey = "OtlpExportIntervalMilliseconds";
            public const string OtlpRuntimeInstrumentationKey = "OtlpRuntimeInstrumentation";
            public const string OtlpTotalNameSuffixForCountersKey = "OtlpTotalNameSuffixForCounters";

            public const int OtlpExportIntervalMillisecondsDefault = 15000;
            public const int OtlpMaxMetricDefault = 4000;
            public const bool OtlpRuntimeInstrumentationDefault = false;
            public const bool OtlpTotalNameSuffixForCountersDefault = false;
            internal const string OtlpEndpointDisabled = "disabled";

            /// <summary>
            /// Use prometheus
            /// </summary>
            public bool AddPrometheusEndpoint
                => GetBoolOrDefault(EnableMetricsKey,
                        GetStringOrDefault(OtlpCollectorEndpointKey) == null);

            /// <summary>
            /// Max metrics to collect, the default in otel is 1000
            /// </summary>
            public int MaxMetricStreams
                => Math.Max(1, GetIntOrDefault(OtlpMaxMetricStreamsKey,
                        OtlpMaxMetricDefault));

            /// <summary>
            /// Add runtime instrumentation
            /// </summary>
            public bool AddRuntimeInstrumentation
                => GetBoolOrDefault(OtlpRuntimeInstrumentationKey,
                        OtlpRuntimeInstrumentationDefault);

            /// <summary>
            /// Enable total suffix
            /// </summary>
            public bool EnableTotalNameSuffixForCounters
                => GetBoolOrDefault(OtlpTotalNameSuffixForCountersKey,
                        OtlpTotalNameSuffixForCountersDefault);

            /// <summary>
            /// Create otlp configuration
            /// </summary>
            /// <param name="configuration"></param>
            public Otlp(IConfiguration configuration)
                : base(configuration)
            {
            }

            /// <inheritdoc/>
            public override void Configure(string? name, OtlpExporterOptions options)
            {
                var endpoint = GetStringOrDefault(OtlpCollectorEndpointKey);
                if (!string.IsNullOrEmpty(endpoint) &&
                    Uri.TryCreate(endpoint, UriKind.RelativeOrAbsolute, out var uri))
                {
                    options.Endpoint = uri;
                }
                else if (endpoint == null)
                {
                    options.Endpoint = new UriBuilder
                    {
                        Scheme = Uri.UriSchemeHttp,
                        Host = OtlpEndpointDisabled
                    }.Uri;
                }
            }

            /// <inheritdoc/>
            public void Configure(string? name, MetricReaderOptions options)
            {
                options.PeriodicExportingMetricReaderOptions =
                    new PeriodicExportingMetricReaderOptions
                    {
                        ExportIntervalMilliseconds = GetIntOrDefault(
                            OtlpExportIntervalMillisecondsKey, OtlpExportIntervalMillisecondsDefault)
                    };
            }

            /// <inheritdoc/>
            public void Configure(MetricReaderOptions options)
            {
                Configure(null, options);
            }
        }

        /// <summary>
        /// Configure kestrel setup
        /// </summary>
        internal sealed class Kestrel : ConfigureOptionBase<KestrelServerOptions>
        {
            /// <summary>
            /// Create kestrel configuration
            /// </summary>
            /// <param name="certificates"></param>
            /// <param name="options"></param>
            /// <param name="configuration"></param>
            public Kestrel(ISslCertProvider certificates, IOptions<PublisherOptions> options,
                IConfiguration configuration)
                : base(configuration)
            {
                _certificates = certificates;
                _options = options;
            }

            /// <inheritdoc/>
            public override void Configure(string? name, KestrelServerOptions options)
            {
                if (_options.Value.UnsecureHttpServerPort != null)
                {
                    options.ListenAnyIP(_options.Value.UnsecureHttpServerPort.Value);
                }

                if (_options.Value.HttpServerPort != null)
                {
                    options.Listen(IPAddress.Any, _options.Value.HttpServerPort.Value,
                        listenOptions => listenOptions.UseHttps(httpsOptions => httpsOptions
                            .ServerCertificateSelector = (_, _) => _certificates.Certificate));
                }
            }

            private readonly IOptions<PublisherOptions> _options;
            private readonly ISslCertProvider _certificates;
        }

        /// <summary>
        /// Configure logger filter
        /// </summary>
        internal sealed class LoggingLevel : ConfigureOptionBase<LoggerFilterOptions>
        {
            /// <summary>
            /// Configuration
            /// </summary>
            public const string LogLevelKey = "LogLevel";

            /// <inheritdoc/>
            public override void Configure(string? name, LoggerFilterOptions options)
            {
                var levelString = GetStringOrDefault(LogLevelKey);
                if (!string.IsNullOrEmpty(levelString))
                {
                    if (Enum.TryParse<LogLevel>(levelString, out var logLevel))
                    {
                        options.MinLevel = logLevel;
                    }
                    else
                    {
                        // Compatibilty with serilog
                        switch (levelString)
                        {
                            case "Verbose":
                                options.MinLevel = LogLevel.Trace;
                                break;
                            case "Fatal":
                                options.MinLevel = LogLevel.Critical;
                                break;
                        }
                    }
                }
            }

            /// <summary>
            /// Create logging configurator
            /// </summary>
            /// <param name="configuration"></param>
            public LoggingLevel(IConfiguration configuration) : base(configuration)
            {
            }
        }

        /// <summary>
        /// Logging format
        /// </summary>
        internal class LoggingFormat : PostConfigureOptionBase<ConsoleLoggerOptions>
        {
            /// <summary>
            /// Supported formats
            /// </summary>
            public static readonly string[] LogFormatsSupported =
            [
                ConsoleFormatterNames.Simple,
                Syslog.FormatterName,
                ConsoleFormatterNames.Systemd
            ];

            /// <summary>
            /// Configuration
            /// </summary>
            public const string LogFormatKey = "LogFormat";

            /// <summary>
            /// Default format
            /// </summary>
            public const string LogFormatDefault = ConsoleFormatterNames.Simple;

            /// <inheritdoc/>
            public override void PostConfigure(string? name, ConsoleLoggerOptions options)
            {
                switch (GetStringOrDefault(LogFormatKey))
                {
                    case Syslog.FormatterName:
                        options.FormatterName = Syslog.FormatterName;
                        break;
                    case ConsoleFormatterNames.Systemd:
                        options.FormatterName = ConsoleFormatterNames.Systemd;
                        break;
                    case ConsoleFormatterNames.Simple:
                        options.FormatterName = ConsoleFormatterNames.Simple;
                        break;
                    default:
                        options.FormatterName = LogFormatDefault;
                        break;
                }
            }

            /// <summary>
            /// Create logging configurator
            /// </summary>
            /// <param name="configuration"></param>
            public LoggingFormat(IConfiguration configuration) : base(configuration)
            {
            }
        }

        /// <summary>
        /// Logging format
        /// </summary>
        /// <typeparam name="T"></typeparam>
        internal sealed class ConsoleLogging<T> : LoggingFormat,
            IConfigureOptions<T>, IConfigureNamedOptions<T> where T : ConsoleFormatterOptions
        {
            /// <inheritdoc/>
            public void Configure(string? name, T options)
            {
                options.TimestampFormat = "[yy-MM-dd HH:mm:ss.ffff] ";
                options.IncludeScopes = true;
                options.UseUtcTimestamp = true;
            }

            /// <inheritdoc/>
            public void Configure(T options)
            {
                Configure(null, options);
            }

            /// <summary>
            /// Create logging configurator
            /// </summary>
            /// <param name="configuration"></param>
            public ConsoleLogging(IConfiguration configuration) : base(configuration)
            {
            }
        }

        /// <summary>
        /// Configure the method router
        /// </summary>
        internal sealed class Router : PostConfigureOptionBase<RouterOptions>
        {
            /// <summary>
            /// Create configuration
            /// </summary>
            /// <param name="configuration"></param>
            /// <param name="options"></param>
            public Router(IConfiguration configuration,
                IOptions<PublisherOptions> options) : base(configuration)
            {
                _options = options;
            }

            /// <inheritdoc/>
            public override void PostConfigure(string? name, RouterOptions options)
            {
                options.MountPoint ??= new TopicBuilder(_options.Value).MethodTopic;
            }

            private readonly IOptions<PublisherOptions> _options;
        }

        /// <summary>
        /// Configure the Dapr event client
        /// </summary>
        internal sealed class Dapr : ConfigureOptionBase<DaprOptions>
        {
            public const string PubSubComponentKey = "PubSubComponent";
            public const string StateStoreKey = "StateStore";
            public const string HttpPortKey = "HttpPort";
            public const string GrpcPortKey = "GrpcPort";
            public const string SchemeKey = "Scheme";
            public const string HostKey = "Host";
            public const string CheckSideCarHealthKey = "CheckSideCarHealth";
            public const string DaprConnectionStringKey = "DaprConnectionString";

            /// <inheritdoc/>
            public override void Configure(string? name, DaprOptions options)
            {
                var daprConnectionString = GetStringOrDefault(DaprConnectionStringKey);
                if (daprConnectionString != null)
                {
                    options.PubSubComponent = null;
                    options.StateStoreName = null;

                    var properties = ToDictionary(daprConnectionString);
                    if (properties.TryGetValue(PubSubComponentKey, out var component))
                    {
                        options.PubSubComponent = component;
                    }

                    if (properties.TryGetValue(StateStoreKey, out var stateStore))
                    {
                        options.StateStoreName = stateStore;
                    }

                    // Select the scheme, default to http (side car)
                    if (!properties.TryGetValue(SchemeKey, out var scheme))
                    {
                        scheme = "http";
                    }

                    // Select the host, default to localhost
                    if (!properties.TryGetValue(HostKey, out var host))
                    {
                        host = "localhost";
                    }

                    // Check whether to check side car health
                    if (properties.TryGetValue(CheckSideCarHealthKey, out var value) &&
                        bool.TryParse(value, out var check))
                    {
                        options.CheckSideCarHealthBeforeAccess = check;
                    }

                    // Permit the port to be set if provided, otherwise use defaults.
                    if (properties.TryGetValue(GrpcPortKey, out value) &&
                        int.TryParse(value, CultureInfo.InvariantCulture, out var port))
                    {
                        options.GrpcEndpoint = scheme + "://" + host + ":" + port;
                    }

                    if (properties.TryGetValue(HttpPortKey, out value) &&
                        int.TryParse(value, CultureInfo.InvariantCulture, out port))
                    {
                        options.HttpEndpoint = scheme + "://" + host + ":" + port;
                    }
                }
                else
                {
                    options.PubSubComponent ??= GetStringOrDefault(PubSubComponentKey);
                    options.StateStoreName ??= GetStringOrDefault(StateStoreKey);
                }

                // The api token should be part of the environment if dapr is supported
                options.ApiToken ??= GetStringOrDefault(EnvironmentVariable.DAPRAPITOKEN);
            }

            /// <summary>
            /// Create configuration
            /// </summary>
            /// <param name="configuration"></param>
            public Dapr(IConfiguration configuration)
                : base(configuration)
            {
            }
        }

        /// <summary>
        /// Configure the http event client
        /// </summary>
        internal sealed class Http : ConfigureOptionBase<HttpEventClientOptions>
        {
            public const string HttpConnectionStringKey = "HttpConnectionString";
            public const string WebHookHostUrlKey = "WebHookHostUrl";
            public const string HostNameKey = "HostName";
            public const string PortKey = "Port";
            public const string SchemeKey = "Scheme";
            public const string PutKey = "Put";

            /// <inheritdoc/>
            public override void Configure(string? name, HttpEventClientOptions options)
            {
                var httpConnectionString = GetStringOrDefault(HttpConnectionStringKey);
                if (httpConnectionString != null)
                {
                    var properties = ToDictionary(httpConnectionString);

                    if (properties.TryGetValue(HostNameKey, out var value))
                    {
                        options.HostName = value;
                    }

                    // Permit the port to be set if provided, otherwise use defaults.
                    if (properties.TryGetValue(PortKey, out value) &&
                        int.TryParse(value, CultureInfo.InvariantCulture, out var port))
                    {
                        options.Port = port;
                    }

                    if (properties.TryGetValue(SchemeKey, out value) &&
                        value.Equals("http", StringComparison.OrdinalIgnoreCase))
                    {
                        options.UseHttpScheme = true;
                    }

                    if (properties.TryGetValue(PutKey, out _))
                    {
                        options.UseHttpPutMethod = true;
                    }
                }
                else
                {
                    var url = GetStringOrDefault(WebHookHostUrlKey);
                    if (!string.IsNullOrEmpty(url) &&
                        Uri.TryCreate(url, UriKind.Absolute, out var uri))
                    {
                        options.HostName = uri.Host;
                        options.Port = uri.Port;
                        options.UseHttpScheme = uri.Scheme == Uri.UriSchemeHttp;
                    }
                }
            }

            /// <summary>
            /// Create configuration
            /// </summary>
            /// <param name="configuration"></param>
            public Http(IConfiguration configuration)
                : base(configuration)
            {
            }
        }

        /// <summary>
        /// Open api configuration
        /// </summary>
        internal sealed class OpenApi : ConfigureOptionBase<OpenApiOptions>
        {
            public const string UseOpenApiV3Key = "UseOpenApiV3";

            /// <inheritdoc/>
            public override void Configure(string? name, OpenApiOptions options)
            {
                options.SchemaVersion = GetBoolOrDefault(UseOpenApiV3Key) ? 3 : 2;
                options.ProjectUri = new Uri("https://www.github.com/Azure/Industrial-IoT");
                options.License = new OpenApiLicense
                {
                    Name = "MIT LICENSE",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                };
            }

            /// <summary>
            /// Create configuration
            /// </summary>
            /// <param name="configuration"></param>
            public OpenApi(IConfiguration configuration)
                : base(configuration)
            {
            }
        }

        /// <summary>
        /// Configure the file based event client
        /// </summary>
        internal sealed class FileSystem : ConfigureOptionBase<FileSystemEventClientOptions>,
            IConfigureOptions<FileSystemRpcServerOptions>,
            IConfigureNamedOptions<FileSystemRpcServerOptions>
        {
            public const string OutputRootKey = "OutputRoot";
            public const string InitFilePathKey = "InitFilePath";
            public const string InitLogFileKey = "InitLogFile";

            /// <inheritdoc/>
            public void Configure(FileSystemRpcServerOptions options)
            {
                Configure(null, options);
            }

            /// <inheritdoc/>
            public void Configure(string? name, FileSystemRpcServerOptions options)
            {
                var publishedNodesFile = GetStringOrDefault(
                    PublisherConfig.PublishedNodesFileKey);
                var rootFolder = Path.GetDirectoryName(publishedNodesFile)
                    ?? Environment.CurrentDirectory;

                options.RequestFilePath ??= GetStringOrDefault(InitFilePathKey);
                if (options.RequestFilePath == null)
                {
                    return;
                }

                if (options.RequestFilePath.Trim().Length == 0)
                {
                    // We use pn.json file path and publishednodes.init file name
                    options.RequestFilePath = Path.Combine(rootFolder,
                        "publishednodes.init");
                }

                // Just file?
                else if (string.IsNullOrEmpty(
                    Path.GetDirectoryName(options.RequestFilePath)))
                {
                    options.RequestFilePath = Path.Combine(rootFolder,
                        options.RequestFilePath!);
                }

                options.ResponseFilePath ??= GetStringOrDefault(InitLogFileKey);
                if (options.ResponseFilePath == null && options.RequestFilePath != null)
                {
                    options.ResponseFilePath = options.RequestFilePath + ".log";
                }

                // Just file?
                else if (string.IsNullOrEmpty(
                    Path.GetDirectoryName(options.ResponseFilePath)))
                {
                    options.ResponseFilePath = Path.Combine(Path.GetDirectoryName(
                        options.RequestFilePath) ?? Environment.CurrentDirectory,
                        options.ResponseFilePath!);
                }
            }

            /// <inheritdoc/>
            public override void Configure(string? name, FileSystemEventClientOptions options)
            {
                options.OutputFolder ??= GetStringOrDefault(OutputRootKey);
            }

            /// <summary>
            /// Create configuration
            /// </summary>
            /// <param name="configuration"></param>
            public FileSystem(IConfiguration configuration)
                : base(configuration)
            {
            }
        }

        /// <summary>
        /// Configure mqtt broker
        /// </summary>
        internal sealed class MqttBroker : ConfigureOptionBase<MqttOptions>
        {
            /// <summary>
            /// Configuration
            /// </summary>
            public const string MqttClientConnectionStringKey = "MqttClientConnectionString";
            public const string ClientPartitionsKey = "MqttClientPartitions";
            public const string KeepAlivePeriodKey = "MqttBrokerKeepAlivePeriod";
            public const string ClientIdKey = "MqttClientId";
            public const string UserNameKey = "MqttBrokerUserName";
            public const string PasswordKey = "MqttBrokerPasswordKey";
            public const string HostNameKey = "MqttBrokerHostName";
            public const string HostPortKey = "MqttBrokerPort";
            public const string ProtocolKey = "MqttProtocolVersion";
            public const string UseTlsKey = "MqttBrokerUsesTls";

            /// <inheritdoc/>
            public override void Configure(string? name, MqttOptions options)
            {
                var mqttClientConnectionString = GetStringOrDefault(MqttClientConnectionStringKey);
                if (mqttClientConnectionString != null)
                {
                    var properties = ToDictionary(mqttClientConnectionString);
                    if (properties.TryGetValue(nameof(options.HostName), out var value))
                    {
                        options.HostName = value;
                    }
                    if (properties.TryGetValue(nameof(options.Port), out value) &&
                        int.TryParse(value, CultureInfo.InvariantCulture, out var port))
                    {
                        options.Port = port;
                    }
                    if (properties.TryGetValue(nameof(options.UserName), out value))
                    {
                        options.UserName = value;
                    }
                    if (properties.TryGetValue(nameof(options.Password), out value))
                    {
                        options.Password = value;
                    }
                    if (properties.TryGetValue(nameof(options.Protocol), out value) &&
                        Enum.TryParse<MqttVersion>(value, true, out var version))
                    {
                        options.Protocol = version;
                    }
                    if (properties.TryGetValue(nameof(options.UseTls), out value) &&
                        bool.TryParse(value, out var useTls))
                    {
                        options.UseTls = useTls;
                    }
                    if (properties.TryGetValue(nameof(options.KeepAlivePeriod), out value) &&
                        TimeSpan.TryParse(value, out var keepAlive))
                    {
                        options.KeepAlivePeriod = keepAlive;
                    }
                    if (properties.TryGetValue("Partitions", out value) &&
                        int.TryParse(value, out var partitions))
                    {
                        options.NumberOfClientPartitions = partitions;
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(options.HostName))
                    {
                        options.ClientId = GetStringOrDefault(HostNameKey);
                    }
                    if (string.IsNullOrEmpty(options.UserName))
                    {
                        options.UserName = GetStringOrDefault(UserNameKey);
                    }
                    if (string.IsNullOrEmpty(options.Password))
                    {
                        options.Password = GetStringOrDefault(PasswordKey);
                    }
                    options.Port ??= GetIntOrNull(HostPortKey);
                    if (Enum.TryParse<MqttVersion>(GetStringOrDefault(ProtocolKey),
                        true, out var version))
                    {
                        options.Protocol = version;
                    }
                    options.UseTls ??= GetBoolOrNull(UseTlsKey);
                    options.NumberOfClientPartitions ??= GetIntOrNull(ClientPartitionsKey);
                    if (options.KeepAlivePeriod == null)
                    {
                        options.KeepAlivePeriod = GetDurationOrNull(KeepAlivePeriodKey);
                    }
                }
                if (string.IsNullOrEmpty(options.ClientId))
                {
                    options.ClientId = GetStringOrDefault(ClientIdKey);
                }
            }

            /// <summary>
            /// Transport configuration
            /// </summary>
            /// <param name="configuration"></param>
            public MqttBroker(IConfiguration configuration)
                : base(configuration)
            {
            }
        }

        /// <summary>
        /// Avro file writer configuration
        /// </summary>
        internal sealed class AvroWriter : ConfigureOptionBase<AvroFileWriterOptions>
        {
            public const string DisableKey = "DisableAvroFileWriter";

            public override void Configure(string? name, AvroFileWriterOptions options)
            {
                options.Disabled = GetBoolOrDefault(DisableKey);
            }

            /// <summary>
            /// Transport configuration
            /// </summary>
            /// <param name="configuration"></param>
            public AvroWriter(IConfiguration configuration)
                : base(configuration)
            {
            }
        }

        /// <summary>
        /// Console file writer configuration
        /// </summary>
        internal sealed class ConsoleWriter : ConfigureOptionBase<ConsoleWriterOptions>
        {
            public const string EnableKey = "EnableConsoleWriter";

            public override void Configure(string? name, ConsoleWriterOptions options)
            {
                options.Enabled = GetBoolOrDefault(EnableKey);
            }

            /// <summary>
            /// Transport configuration
            /// </summary>
            /// <param name="configuration"></param>
            public ConsoleWriter(IConfiguration configuration)
                : base(configuration)
            {
            }
        }

        /// <summary>
        /// Configure schema topic templates
        /// </summary>
        internal sealed class SchemaTopicBuilder : ConfigureOptionBase<MqttOptions>
        {
            /// <inheritdoc/>
            public override void Configure(string? name, MqttOptions options)
            {
                if (_options.Value.SchemaOptions != null &&
                    _options.Value.TopicTemplates.Schema != null)
                {
                    options.ConfigureSchemaMessage = message =>
                    {
                        //
                        // Set the telemetry topic template to the passed
                        // in message topic
                        //
                        var templates = new TopicTemplatesOptions
                        {
                            Telemetry = message.Topic
                        };
                        message.Topic = new TopicBuilder(_options.Value,
                            templates: templates).SchemaTopic;
                    };
                }
            }

            /// <inheritdoc/>
            public SchemaTopicBuilder(IConfiguration configuration,
                IOptions<PublisherOptions> options)
                : base(configuration)
            {
                _options = options;
            }

            private readonly IOptions<PublisherOptions> _options;
        }

        /// <summary>
        /// Configure edge client
        /// </summary>
        internal sealed class IoTEdge : ConfigureOptionBase<IoTEdgeClientOptions>
        {
            /// <summary>
            /// Configuration
            /// </summary>
            public const string HubTransport = "Transport";
            public const string UpstreamProtocol = "UpstreamProtocol";
            public const string EdgeHubConnectionString = "EdgeHubConnectionString";

            /// <inheritdoc/>
            public override void Configure(string? name, IoTEdgeClientOptions options)
            {
                if (string.IsNullOrEmpty(options.EdgeHubConnectionString))
                {
                    options.EdgeHubConnectionString = GetStringOrDefault(EdgeHubConnectionString);
                }
                if (options.EdgeHubConnectionString == null &&
                    Environment.GetEnvironmentVariable("IOTEDGE_DEVICEID") != null)
                {
                    options.EdgeHubConnectionString = string.Empty;
                }
                if (options.Transport == TransportOption.None)
                {
                    if (Enum.TryParse<TransportOption>(GetStringOrDefault(HubTransport),
                            out var transport) ||
                        Enum.TryParse(GetStringOrDefault(UpstreamProtocol),
                            out transport))
                    {
                        options.Transport = transport;
                    }
                }
                options.Product = $"OpcPublisher_{GetType().Assembly.GetReleaseVersion()}";
            }

            /// <summary>
            /// Transport configuration
            /// </summary>
            /// <param name="configuration"></param>
            public IoTEdge(IConfiguration configuration)
                : base(configuration)
            {
            }
        }

        /// <summary>
        /// Configure event hub client
        /// </summary>
        internal sealed class EventHubs : ConfigureOptionBase<EventHubsClientOptions>
        {
            /// <summary>
            /// Configuration
            /// </summary>
            public const string SchemaGroupNameKey = "SchemaGroupName";
            public const string EventHubNamespaceConnectionString = "EventHubNamespaceConnectionString";

            /// <inheritdoc/>
            public override void Configure(string? name, EventHubsClientOptions options)
            {
                if (string.IsNullOrEmpty(options.ConnectionString))
                {
                    options.ConnectionString = GetStringOrDefault(EventHubNamespaceConnectionString);
                }

                var schemaGroupName = GetStringOrDefault(SchemaGroupNameKey);

                if (!string.IsNullOrEmpty(schemaGroupName))
                {
                    options.SchemaRegistry = new SchemaRegistryOptions
                    {
                        FullyQualifiedNamespace = string.Empty, // TODO: Remove
                        SchemaGroupName = schemaGroupName
                    };
                }
            }

            /// <summary>
            /// Transport configuration
            /// </summary>
            /// <param name="configuration"></param>
            public EventHubs(IConfiguration configuration)
                : base(configuration)
            {
            }
        }

        /// <summary>
        /// Parse connection string as dictionary
        /// </summary>
        /// <param name="valuePairString"></param>
        /// <param name="kvpDelimiter"></param>
        /// <param name="kvpSeparator"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="FormatException"></exception>
        private static Dictionary<string, string> ToDictionary(string valuePairString,
            char kvpDelimiter = ';', char kvpSeparator = '=')
        {
            if (string.IsNullOrWhiteSpace(valuePairString))
            {
                throw new ArgumentException("Malformed Token");
            }

            valuePairString = valuePairString.Trim(';');

            // This regex allows semi-colons to be part of the allowed characters
            // for device names. Although new devices are not
            // allowed to have semi-colons in the name, some legacy devices still
            // have them and so this name validation cannot be changed.
            var parts = new Regex($"(?:^|{kvpDelimiter})([^{kvpDelimiter}{kvpSeparator}]*){kvpSeparator}")
                .Matches(valuePairString)
                .Cast<Match>()
                .Select(m => new string[] {
                    m.Result("$1"),
                    valuePairString[
                        (m.Index + m.Value.Length)..(m.NextMatch().Success ? m.NextMatch().Index : valuePairString.Length)]
                })
                .ToList();

            if (parts.Count == 0 || parts.Any(p => p.Length != 2))
            {
                throw new FormatException("Malformed Token");
            }
            return parts.ToDictionary(kvp => kvp[0], (kvp) => kvp[1], StringComparer.OrdinalIgnoreCase);
        }
    }
}
