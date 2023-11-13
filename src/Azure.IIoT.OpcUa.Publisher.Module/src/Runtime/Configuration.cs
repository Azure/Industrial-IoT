// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Runtime
{
    using Azure.IIoT.OpcUa.Publisher.Module.Controllers;
    using Autofac;
    using Furly.Azure.IoT.Edge;
    using Furly.Azure.IoT.Edge.Services;
    using Furly.Extensions.AspNetCore.OpenApi;
    using Furly.Extensions.Configuration;
    using Furly.Extensions.Dapr;
    using Furly.Extensions.Logging;
    using Furly.Extensions.Messaging.Runtime;
    using Furly.Extensions.Mqtt;
    using Furly.Tunnel.Router.Services;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Server.Kestrel.Core;
    using Microsoft.AspNetCore.Server.Kestrel.Https;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.OpenApi.Models;
    using OpenTelemetry.Exporter;
    using OpenTelemetry.Logs;
    using OpenTelemetry.Metrics;
    using OpenTelemetry.Trace;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
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
            builder.RegisterType<Logging>()
                .AsImplementedInterfaces();
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
            builder.RegisterType<GeneralController>()
                .AsImplementedInterfaces();
            builder.RegisterType<HistoryController>()
                .AsImplementedInterfaces();
            builder.RegisterType<DiscoveryController>()
                .AsImplementedInterfaces();
            builder.RegisterType<CertificatesController>()
                .AsImplementedInterfaces();
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
        /// Add file system client
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        public static void AddFileSystemEventClient(this ContainerBuilder builder,
            IConfiguration configuration)
        {
            var fsOptions = new FileSystemOptions();
            new FileSystem(configuration).Configure(fsOptions);
            if (fsOptions.OutputFolder != null)
            {
                builder.AddFileSystemEventClient();
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
            var httpOptions = new HttpOptions();
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
            if (daprOptions.PubSubComponent != null)
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
            if (daprOptions.StateStoreName != null)
            {
                builder.AddDaprStateStoreClient();
                builder.RegisterType<Dapr>()
                    .AsImplementedInterfaces();
            }
        }

        /// <summary>
        /// Add otlp exporter if configured
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        public static MeterProviderBuilder AddOtlpExporter(this MeterProviderBuilder builder,
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
        /// Add prometheus exporter if configured
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        public static MeterProviderBuilder AddPrometheusExporter(this MeterProviderBuilder builder,
            IConfiguration configuration)
        {
            if (new Otlp(configuration).AddPrometheusEndpoint)
            {
                builder.ConfigureServices(services => services.AddSingleton<Otlp>());
                builder.AddPrometheusExporter();
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
        /// Otlp configuration from environment
        /// </summary>
        internal sealed class Otlp : ConfigureOptionBase<OtlpExporterOptions>,
            IConfigureOptions<MetricReaderOptions>, IConfigureNamedOptions<MetricReaderOptions>
        {
            public const string OtlpCollectorEndpointKey = "OtlpCollectorEndpoint";
            public const string EnableMetricsKey = "EnableMetrics";
            public const string OtlpExportIntervalMillisecondsKey = "OtlpExportIntervalMilliseconds";
            internal const string OtlpEndpointDisabled = "disabled";
            public const int OtlpExportIntervalMillisecondsDefault = 15000;

            /// <summary>
            /// Use prometheus
            /// </summary>
            public bool AddPrometheusEndpoint
            {
                get
                {
                    return GetBoolOrDefault(EnableMetricsKey,
                        GetStringOrDefault(OtlpCollectorEndpointKey) == null);
                }
            }

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
            /// Running in container
            /// </summary>
            static bool IsContainer => StringComparer.OrdinalIgnoreCase.Equals(
                Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER")
                    ?? string.Empty, "true");

            /// <summary>
            /// Configuration
            /// </summary>
            public const string HttpServerPortKey = "HttpServerPort";
            public const string UnsecureHttpServerPortKey = "UnsecureHttpServerPort";

            public static readonly int HttpPortDefault = IsContainer ? 80 : 9071;
            public static readonly int HttpsPortDefault = IsContainer ? 443 : 9072;

            /// <summary>
            /// Create kestrel configuration
            /// </summary>
            /// <param name="certificates"></param>
            /// <param name="configuration"></param>
            public Kestrel(ISslCertProvider certificates, IConfiguration configuration)
                : base(configuration)
            {
                _certificates = certificates;
            }

            /// <inheritdoc/>
            public override void Configure(string? name, KestrelServerOptions options)
            {
                var httpPort = GetIntOrNull(UnsecureHttpServerPortKey);
                options.ListenAnyIP(httpPort ?? HttpPortDefault);

                var httpsPort = GetIntOrNull(HttpServerPortKey);
                options.Listen(IPAddress.Any, httpsPort ?? HttpsPortDefault, listenOptions
                    => listenOptions.UseHttps(httpsOptions => httpsOptions
                        .ServerCertificateSelector = (_, _) => _certificates.Certificate));
            }

            private readonly ISslCertProvider _certificates;
        }

        /// <summary>
        /// Configure logger factory
        /// </summary>
        internal sealed class Logging : ConfigureOptionBase<LoggerFilterOptions>
        {
            /// <summary>
            /// Configuration
            /// </summary>
            public const string LogLevelKey = "LogLevel";

            /// <inheritdoc/>
            public override void Configure(string? name, LoggerFilterOptions options)
            {
                if (Enum.TryParse<LogLevel>(GetStringOrDefault(LogLevelKey), out var logLevel))
                {
                    options.MinLevel = logLevel;
                }
            }

            /// <summary>
            /// Create logging configurator
            /// </summary>
            /// <param name="configuration"></param>
            public Logging(IConfiguration configuration) : base(configuration)
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
                if (options.MountPoint == null)
                {
                    options.MountPoint = new TopicBuilder(_options).MethodTopic;
                }
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
            public const string DaprConnectionStringKey = "DaprConnectionString";

            /// <inheritdoc/>
            public override void Configure(string? name, DaprOptions options)
            {
                var daprConnectionString = GetStringOrDefault(DaprConnectionStringKey);
                if (daprConnectionString != null)
                {
                    options.PubSubComponent = string.Empty;
                    options.StateStoreName = string.Empty;

                    var properties = ToDictionary(daprConnectionString);
                    if (properties.TryGetValue(PubSubComponentKey, out var component))
                    {
                        options.PubSubComponent = component;
                    }

                    if (properties.TryGetValue(StateStoreKey, out var stateStore))
                    {
                        options.StateStoreName = stateStore;
                    }

                    // Permit the port to be set if provided, otherwise use defaults.
                    if (properties.TryGetValue(GrpcPortKey, out var value) &&
                        int.TryParse(value, CultureInfo.InvariantCulture, out var port))
                    {
                        options.GrpcEndpoint = "https://localhost:" + port;
                    }

                    if (properties.TryGetValue(HttpPortKey, out value) &&
                        int.TryParse(value, CultureInfo.InvariantCulture, out port))
                    {
                        options.HttpEndpoint = "https://localhost:" + port;
                    }
                }
                else
                {
                    options.PubSubComponent ??= GetStringOrDefault(PubSubComponentKey);
                    options.StateStoreName ??= GetStringOrDefault(StateStoreKey);
                }

                // The api token should be part of the environment if dapr is supported
                if (options.ApiToken == null)
                {
                    options.ApiToken = GetStringOrDefault(EnvironmentVariable.DAPRAPITOKEN);
                }
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
        internal sealed class Http : ConfigureOptionBase<HttpOptions>
        {
            public const string HttpConnectionStringKey = "HttpConnectionString";
            public const string WebHookHostUrlKey = "WebHookHostUrl";
            public const string HostNameKey = "HostName";
            public const string PortKey = "Port";
            public const string SchemeKey = "Scheme";
            public const string PutKey = "Put";

            /// <inheritdoc/>
            public override void Configure(string? name, HttpOptions options)
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
            public const string DisableSwaggerUIKey = "DisableSwaggerUI";
            public const string UseOpenApiV3Key = "UseOpenApiV3";

            /// <inheritdoc/>
            public override void Configure(string? name, OpenApiOptions options)
            {
                if (_isDisabled)
                {
                    options.UIEnabled = false;
                }
                else
                {
                    var uiEnabled = GetBoolOrNull(DisableSwaggerUIKey);
                    if (uiEnabled != null)
                    {
                        options.UIEnabled = uiEnabled.Value;
                    }

                    var useV3 = GetBoolOrNull(UseOpenApiV3Key);
                    if (useV3 != null)
                    {
                        options.SchemaVersion = useV3.Value ? 3 : 2;
                    }
                }
            }

            /// <summary>
            /// Create configuration
            /// </summary>
            /// <param name="configuration"></param>
            /// <param name="options"></param>
            public OpenApi(IConfiguration configuration,
                IOptions<PublisherOptions>? options = null)
                : base(configuration)
            {
                _isDisabled = options?.Value.DisableOpenApiEndpoint == true;
            }

            private readonly bool _isDisabled;
        }

        /// <summary>
        /// Configure the file based event client
        /// </summary>
        internal sealed class FileSystem : ConfigureOptionBase<FileSystemOptions>
        {
            public const string OutputRootKey = "OutputRoot";

            /// <inheritdoc/>
            public override void Configure(string? name, FileSystemOptions options)
            {
                if (options.OutputFolder == null)
                {
                    options.OutputFolder = GetStringOrDefault(OutputRootKey);
                }
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
                    if (properties.ContainsKey(nameof(options.UserName)))
                    {
                        options.UserName = properties[nameof(options.UserName)];
                    }
                    if (properties.ContainsKey(nameof(options.Password)))
                    {
                        options.Password = properties[nameof(options.Password)];
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
                    if (options.Port == null)
                    {
                        options.Port = GetIntOrNull(HostPortKey);
                    }
                    if (Enum.TryParse<MqttVersion>(GetStringOrDefault(ProtocolKey),
                        true, out var version))
                    {
                        options.Protocol = version;
                    }
                    if (options.UseTls == null)
                    {
                        options.UseTls = GetBoolOrNull(UseTlsKey);
                    }
                    if (options.NumberOfClientPartitions == null)
                    {
                        options.NumberOfClientPartitions = GetIntOrNull(ClientPartitionsKey);
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
        /// Parse connection string as dictionary
        /// </summary>
        /// <param name="valuePairString"></param>
        /// <param name="kvpDelimiter"></param>
        /// <param name="kvpSeparator"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="FormatException"></exception>
        private static IDictionary<string, string> ToDictionary(string valuePairString,
            char kvpDelimiter = ';', char kvpSeparator = '=')
        {
            if (string.IsNullOrWhiteSpace(valuePairString))
            {
                throw new ArgumentException("Malformed Token");
            }

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
                });

            if (!parts.Any() || parts.Any(p => p.Length != 2))
            {
                throw new FormatException("Malformed Token");
            }
            return parts.ToDictionary(kvp => kvp[0], (kvp) => kvp[1], StringComparer.OrdinalIgnoreCase);
        }
    }
}
