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
    using Furly.Extensions.Configuration;
    using Furly.Extensions.Dapr;
    using Furly.Extensions.Logging;
    using Furly.Extensions.Messaging.Runtime;
    using Furly.Extensions.Mqtt;
    using Furly.Tunnel.Router.Services;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Server.Kestrel.Core;
    using Microsoft.AspNetCore.Server.Kestrel.Https;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
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

            builder.RegisterType<PublisherMethodsController>()
                .AsImplementedInterfaces();
            builder.RegisterType<TwinMethodsController>()
                .AsImplementedInterfaces();
            builder.RegisterType<HistoryMethodsController>()
                .AsImplementedInterfaces();
            builder.RegisterType<DiscoveryMethodsController>()
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
        public static void AddDaprClient(this ContainerBuilder builder,
            IConfiguration configuration)
        {
            var daprOptions = new DaprOptions();
            new Dapr(configuration).Configure(daprOptions);
            if (daprOptions.PubSubComponent != null &&
                daprOptions.ApiToken != null)
            {
                builder.AddDaprClient();
                builder.RegisterType<Dapr>()
                    .AsImplementedInterfaces();
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
            public const string DisableHttpServerKey = "DisableHttpServer";
            public const string HttpServerPortKey = "HttpServerPort";
            public const string UnsecureHttpServerPortKey = "UnsecureHttpServerPort";

            public static readonly int HttpPortDefault = IsContainer ? 80 : 9071;
            public static readonly int HttpsPortDefault = IsContainer ? 443 : 9072;

            /// <summary>
            /// Create kestrel configuration
            /// </summary>
            /// <param name="memoryCache"></param>
            /// <param name="configuration"></param>
            /// <param name="workload"></param>
            public Kestrel(IMemoryCache memoryCache, IConfiguration configuration,
                IoTEdgeWorkloadApi? workload = null)
                : base(configuration)
            {
                _memoryCache = memoryCache;
                _workload = workload;
            }

            /// <inheritdoc/>
            public override void Configure(string? name, KestrelServerOptions options)
            {
                var disableHttp = GetBoolOrDefault(DisableHttpServerKey);
                if (disableHttp)
                {
                    return;
                }

                var httpPort = GetIntOrNull(UnsecureHttpServerPortKey);
                options.ListenAnyIP(httpPort ?? HttpPortDefault);

                var httpsPort = GetIntOrNull(HttpServerPortKey);
                options.Listen(IPAddress.Any, httpsPort ?? HttpsPortDefault, listenOptions =>
                {
                    listenOptions.UseHttps(httpsOptions =>
                    {
                        httpsOptions.ServerCertificateSelector = (_, dnsName) =>
                        {
                            dnsName ??= "certificate";
                            try
                            {
                                // Try get or create new certificate
                                return GetCertificate(dnsName, httpsOptions);
                            }
                            catch
                            {
                                // Invalidate
                                _memoryCache.Remove(dnsName);
                                return httpsOptions.ServerCertificate;
                            }
                        };
                    });
                });

                X509Certificate2? GetCertificate(string dnsName, HttpsConnectionAdapterOptions httpsOptions)
                {
                    return _memoryCache.GetOrCreate(dnsName, async cacheEntry =>
                    {
                        if (_workload != null)
                        {
                            cacheEntry.AbsoluteExpirationRelativeToNow =
                                TimeSpan.FromDays(30);
                            var expiration = DateTime.UtcNow +
                                cacheEntry.AbsoluteExpirationRelativeToNow.Value;
                            var chain = await _workload.CreateServerCertificateAsync(
                                dnsName, expiration, default).ConfigureAwait(false);
                            // TODO: where should the chain go?
                            httpsOptions.ServerCertificateChain =
                                new X509Certificate2Collection(chain.Skip(1).ToArray());
                            return chain[0];
                        }
                        return null;
                    })?.Result;
                }
            }

            private readonly IMemoryCache _memoryCache;
            private readonly IoTEdgeWorkloadApi? _workload;
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
            public const string HttpPortKey = "HttpPort";
            public const string GrpcPortKey = "GrpcPort";
            public const string DaprConnectionStringKey = "DaprConnectionString";

            /// <inheritdoc/>
            public override void Configure(string? name, DaprOptions options)
            {
                var daprConnectionString = GetStringOrDefault(DaprConnectionStringKey);
                if (daprConnectionString != null)
                {
                    var properties = ToDictionary(daprConnectionString);
                    options.PubSubComponent = properties[PubSubComponentKey];

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
                    options.HostName = properties[HostNameKey];

                    // Permit the port to be set if provided, otherwise use defaults.
                    if (properties.TryGetValue(PortKey, out var value) &&
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
                    options.HostName = properties[nameof(options.HostName)];

                    // Permit the port to be set if provided, otherwise use defaults.
                    if (properties.TryGetValue(nameof(options.Port), out var value) &&
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
                    if (properties.TryGetValue(nameof(options.Version), out value) &&
                        Enum.TryParse<MqttVersion>(value, true, out var version))
                    {
                        options.Version = version;
                    }
                    if (properties.TryGetValue(nameof(options.UseTls), out value) &&
                        bool.TryParse(value, out var useTls))
                    {
                        options.UseTls = useTls;
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
                        options.Version = version;
                    }
                    if (options.UseTls == null)
                    {
                        options.UseTls = GetBoolOrNull(UseTlsKey);
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
                        Enum.TryParse<TransportOption>(GetStringOrDefault(UpstreamProtocol),
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
