// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Runtime
{
    using Azure.IIoT.OpcUa.Publisher.Module.Controllers;
    using Autofac;
    using Furly.Azure.IoT.Edge;
    using Furly.Extensions.Configuration;
    using Furly.Extensions.Logging;
    using Furly.Extensions.Mqtt;
    using Furly.Tunnel.Router.Services;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
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
                //  builder.AddMqttClient();
                builder.RegisterType<Furly.Extensions.Mqtt.Clients.MqttClient>().AsImplementedInterfaces().SingleInstance();
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
        /// Configure logger factory
        /// </summary>
        internal sealed class Logging : ConfigureOptionBase<LoggerFilterOptions>
        {
            /// <summary>
            /// Configuration
            /// </summary>
            public const string LogLevelKey = "LogLevel";
            public const LogLevel LogLevelDefault = LogLevel.Information;

            /// <inheritdoc/>
            public override void Configure(string name, LoggerFilterOptions options)
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
            public override void PostConfigure(string name, RouterOptions options)
            {
                if (options.MountPoint == null)
                {
                    options.MountPoint = new TopicBuilder(_options).MethodTopic;
                }
            }

            private readonly IOptions<PublisherOptions> _options;
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
            public override void Configure(string name, MqttOptions options)
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
            public const string EdgeHubConnectionString = "EdgeHubConnectionString";
            public const string BypassCertVerificationKey = "BypassCertVerification";

            /// <inheritdoc/>
            public override void Configure(string name, IoTEdgeClientOptions options)
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
                if (options.Transport == TransportOption.None &&
                    Enum.TryParse<TransportOption>(GetStringOrDefault(HubTransport),
                        out var transport))
                {
                    options.Transport = transport;
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
                    valuePairString.Substring(
                        m.Index + m.Value.Length,
                        (m.NextMatch().Success ? m.NextMatch().Index : valuePairString.Length)
                            - (m.Index + m.Value.Length))
                });

            if (!parts.Any() || parts.Any(p => p.Length != 2))
            {
                throw new FormatException("Malformed Token");
            }
            return parts.ToDictionary(kvp => kvp[0], (kvp) => kvp[1], StringComparer.OrdinalIgnoreCase);
        }
    }
}
