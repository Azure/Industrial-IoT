// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Runtime
{
    using Azure.IIoT.OpcUa.Core.Messaging.Clients;
    using Azure.IIoT.OpcUa.Core.Messaging.Clients.Dapr;
    using Azure.IIoT.OpcUa.Core.Messaging.Clients.EventHubs;
    using Azure.IIoT.OpcUa.Core.Messaging.Clients.IoTEdge;
    using Azure.IIoT.OpcUa.Core.Messaging.Clients.Mqtt;
    using Azure.IIoT.OpcUa.Core.Rpc.Router;
    using Azure.IIoT.OpcUa.Core.Rpc.Servers;
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Module.Runtime;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Console;
    using Microsoft.Extensions.Options;
    using OpenTelemetry.Exporter;
    using OpenTelemetry.Metrics;
    using System;
    using System.Collections.Generic;
    using Xunit;

    [Collection("EnvironmentVariables")]
    public sealed class ConfigurationTests
    {
        // The MCP tool server is served by the module's own http listeners, so
        // asking for it must guarantee one exists rather than silently starting
        // an endpoint nobody can reach.
        [Fact]
        public void McpServerEnablesTheDefaultHttpPortWhenNoneIsConfigured()
        {
            var options = new PublisherOptions
            {
                EnableMcpServer = true,
                HttpServerPort = null,
                UnsecureHttpServerPort = null
            };

            var (unsecurePort, securePort) = Configuration.Kestrel.GetListenPorts(options);

            Assert.Null(unsecurePort);
            Assert.Equal(PublisherConfig.HttpServerPortDefault, securePort);
        }

        [Fact]
        public void McpServerDoesNotOverrideAConfiguredHttpPort()
        {
            var options = new PublisherOptions
            {
                EnableMcpServer = true,
                HttpServerPort = 8443,
                UnsecureHttpServerPort = null
            };

            var (unsecurePort, securePort) = Configuration.Kestrel.GetListenPorts(options);

            Assert.Null(unsecurePort);
            Assert.Equal(8443, securePort);
        }

        // An unsecure listener already satisfies the requirement, so no second
        // listener is opened behind the operator's back.
        [Fact]
        public void McpServerIsSatisfiedByAnUnsecureListener()
        {
            var options = new PublisherOptions
            {
                EnableMcpServer = true,
                HttpServerPort = null,
                UnsecureHttpServerPort = 9071
            };

            var (unsecurePort, securePort) = Configuration.Kestrel.GetListenPorts(options);

            Assert.Equal(9071, unsecurePort);
            Assert.Null(securePort);
        }

        // Without the flag nothing changes: no listener is conjured up.
        [Fact]
        public void WithoutMcpServerNoPortIsAdded()
        {
            var options = new PublisherOptions
            {
                EnableMcpServer = null,
                HttpServerPort = null,
                UnsecureHttpServerPort = null
            };

            var (unsecurePort, securePort) = Configuration.Kestrel.GetListenPorts(options);

            Assert.Null(unsecurePort);
            Assert.Null(securePort);
        }

        [Fact]
        public void OtelDefaultsEnablePrometheusMetrics()
        {
            var configurator = new Configuration.Otel(CreateConfiguration());

            Assert.Equal(true, configurator.EnableMetrics);
            Assert.Equal(true, configurator.AddPrometheusEndpoint);
            Assert.Equal(false, configurator.EnableOtelMetrics);
            Assert.Equal(false, configurator.EnableOtelLogging);
            Assert.Equal(false, configurator.EnableOtelTraces);
            Assert.Equal(false, configurator.AddRuntimeInstrumentation);
            Assert.Equal(false, configurator.EnableTotalNameSuffixForCounters);
            Assert.Equal(Configuration.Otel.OtlpMaxMetricDefault,
                configurator.MaxMetricStreams);

            var reader = new MetricReaderOptions();
            configurator.Configure(null, reader);

            Assert.Equal(Configuration.Otel.OtlpExportIntervalMillisecondsDefault,
                reader.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds);
        }

        [Fact]
        public void OtelCollectorEndpointConfiguresEverySignal()
        {
            var configurator = new Configuration.Otel(CreateConfiguration(new()
            {
                [Configuration.Otel.OtlpCollectorEndpointKey] = "grpc://collector:4317",
                [Configuration.Otel.EnableOtelLoggingKey] = "yes",
                [Configuration.Otel.EnableOtelTracesKey] = "1",
                [Configuration.Otel.OtlpMaxMetricStreamsKey] = "0",
                [Configuration.Otel.OtlpExportIntervalMillisecondsKey] = "123",
                [Configuration.Otel.OtlpRuntimeInstrumentationKey] = "true",
                [Configuration.Otel.OtlpTotalNameSuffixForCountersKey] = "true"
            }));

            Assert.Equal(false, configurator.AddPrometheusEndpoint);
            Assert.Equal(true, configurator.EnableOtelMetrics);
            Assert.Equal(true, configurator.EnableOtelLogging);
            Assert.Equal(true, configurator.EnableOtelTraces);
            Assert.Equal(true, configurator.AddRuntimeInstrumentation);
            Assert.Equal(true, configurator.EnableTotalNameSuffixForCounters);
            Assert.Equal(1, configurator.MaxMetricStreams);

            var exporter = new OtlpExporterOptions();
            configurator.Configure("Metrics", exporter);
            Assert.Equal(new Uri("http://collector:4317/"), exporter.Endpoint);

            exporter = new OtlpExporterOptions();
            configurator.Configure("Traces", exporter);
            Assert.Equal(new Uri("http://collector:4317/"), exporter.Endpoint);

            exporter = new OtlpExporterOptions();
            configurator.Configure("Logging", exporter);
            Assert.Equal(new Uri("http://collector:4317/"), exporter.Endpoint);

            var reader = new MetricReaderOptions();
            configurator.Configure(null, reader);
            Assert.Equal(123,
                reader.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds);
        }

        [Fact]
        public void OtelUsesLegacyPerSignalEndpoints()
        {
            var configurator = new Configuration.Otel(CreateConfiguration(new()
            {
                ["OTLP_GRPC_METRIC_ENDPOINT"] = "https://metrics:4317",
                ["OTLP_HTTP_TRACE_ENDPOINT"] = "https://traces:4318",
                ["OTLP_GRPC_LOG_ENDPOINT"] = "https://logs:4317",
                [Configuration.Otel.EnableOtelLoggingKey] = "true",
                [Configuration.Otel.EnableOtelTracesKey] = "true"
            }));

            Assert.Equal(true, configurator.EnableOtelMetrics);
            Assert.Equal(true, configurator.EnableOtelLogging);
            Assert.Equal(true, configurator.EnableOtelTraces);
            Assert.Equal("https://metrics:4317", configurator.OTlpMetricsEndpoint);
            Assert.Equal("https://traces:4318", configurator.OTlpTracesEndpoint);
            Assert.Equal("https://logs:4317", configurator.OTlpLogEndpoint);
        }

        [Theory]
        [InlineData("Warning", LogLevel.Warning)]
        [InlineData("Verbose", LogLevel.Trace)]
        [InlineData("Fatal", LogLevel.Critical)]
        public void LoggingLevelUsesCommandLineValueBeforeDiagnostics(
            string configured, LogLevel expected)
        {
            var options = new LoggerFilterOptions();
            var configurator = new Configuration.LoggingLevel(CreateConfiguration(new()
            {
                [Configuration.LoggingLevel.LogLevelKey] = configured,
                ["logs:level"] = "debug"
            }));

            configurator.Configure(null, options);

            Assert.Equal(expected, options.MinLevel);
        }

        [Theory]
        [InlineData("trace", LogLevel.Trace)]
        [InlineData("debug", LogLevel.Debug)]
        [InlineData("info", LogLevel.Information)]
        [InlineData("warn", LogLevel.Warning)]
        [InlineData("error", LogLevel.Error)]
        [InlineData("none", LogLevel.None)]
        [InlineData("Critical", LogLevel.Critical)]
        public void LoggingLevelUsesDiagnosticsValueWhenCommandLineIsAbsent(
            string configured, LogLevel expected)
        {
            var options = new LoggerFilterOptions();
            var configurator = new Configuration.LoggingLevel(CreateConfiguration(new()
            {
                ["logs:level"] = configured
            }));

            configurator.Configure(null, options);

            Assert.Equal(expected, options.MinLevel);
        }

        [Theory]
        [InlineData("syslog", "syslog")]
        [InlineData("systemd", "systemd")]
        [InlineData("simple", "simple")]
        [InlineData("unsupported", "simple")]
        [InlineData(null, "simple")]
        public void LoggingFormatSelectsSupportedFormatterOrDefault(
            string configured, string expected)
        {
            var options = new ConsoleLoggerOptions();
            var configuration = configured == null
                ? CreateConfiguration()
                : CreateConfiguration(new()
                {
                    [Configuration.LoggingFormat.LogFormatKey] = configured
                });

            new Configuration.LoggingFormat(configuration).PostConfigure(null, options);

            Assert.Equal(expected, options.FormatterName);
        }

        [Fact]
        public void ConsoleLoggingConfiguresFormatterOptions()
        {
            var options = new ConsoleFormatterOptions();

            new Configuration.ConsoleLogging<ConsoleFormatterOptions>(
                CreateConfiguration()).Configure(null, options);

            Assert.Equal("[yy-MM-dd HH:mm:ss.ffff] ", options.TimestampFormat);
            Assert.Equal(true, options.IncludeScopes);
            Assert.Equal(true, options.UseUtcTimestamp);
        }

        [Fact]
        public void DaprConnectionStringConfiguresEndpointsComponentsAndToken()
        {
            var options = new DaprOptions();
            var configurator = new Configuration.Dapr(CreateConfiguration(new()
            {
                [Configuration.Dapr.DaprConnectionStringKey] =
                    "PubSubComponent=pub;StateStore=state;Scheme=https;Host=dapr;" +
                    "GrpcPort=50001;HttpPort=3500;CheckSideCarHealth=true",
                [EnvironmentVariable.DAPRAPITOKEN] = "token"
            }));

            configurator.Configure(null, options);

            Assert.Equal("pub", options.PubSubComponent);
            Assert.Equal("state", options.StateStoreName);
            Assert.Equal("https://dapr:50001", options.GrpcEndpoint);
            Assert.Equal("https://dapr:3500", options.HttpEndpoint);
            Assert.Equal(true, options.CheckSideCarHealthBeforeAccess);
            Assert.Equal("token", options.ApiToken);
        }

        [Fact]
        public void DaprFallbackKeysDoNotOverwriteExistingComponent()
        {
            var options = new DaprOptions
            {
                PubSubComponent = "existing"
            };
            var configurator = new Configuration.Dapr(CreateConfiguration(new()
            {
                [Configuration.Dapr.PubSubComponentKey] = "configured",
                [Configuration.Dapr.StateStoreKey] = "state"
            }));

            configurator.Configure(null, options);

            Assert.Equal("existing", options.PubSubComponent);
            Assert.Equal("state", options.StateStoreName);
        }

        [Fact]
        public void HttpConnectionStringConfiguresEndpointAndMethod()
        {
            var options = new HttpEventClientOptions();
            var configurator = new Configuration.Http(CreateConfiguration(new()
            {
                [Configuration.Http.HttpConnectionStringKey] =
                    "HostName=hook;Port=8080;Scheme=http;Put=true"
            }));

            configurator.Configure(null, options);

            Assert.Equal("hook", options.HostName);
            Assert.Equal(8080, options.Port);
            Assert.Equal(true, options.UseHttpScheme);
            Assert.Equal(true, options.UseHttpPutMethod);
        }

        [Fact]
        public void HttpWebhookUrlConfiguresHostPortAndScheme()
        {
            var options = new HttpEventClientOptions();
            var configurator = new Configuration.Http(CreateConfiguration(new()
            {
                [Configuration.Http.WebHookHostUrlKey] = "https://example.test:9443/path"
            }));

            configurator.Configure(null, options);

            Assert.Equal("example.test", options.HostName);
            Assert.Equal(9443, options.Port);
            Assert.Equal(false, options.UseHttpScheme);
        }

        [Fact]
        public void FileSystemConfiguresOutputAndInitPathsRelativeToPublishedNodes()
        {
            var options = new FileSystemRpcServerOptions();
            var configurator = new Configuration.FileSystem(CreateConfiguration(new()
            {
                [PublisherConfig.PublishedNodesFileKey] =
                    TestPaths.Rooted("publisher", "config", "publishednodes.json"),
                [Configuration.FileSystem.InitFilePathKey] = "requests.http",
                [Configuration.FileSystem.InitLogFileKey] = "responses.log"
            }));

            configurator.Configure(null, options);

            Assert.Equal(TestPaths.Rooted("publisher", "config", "requests.http"), options.RequestFilePath);
            Assert.Equal(TestPaths.Rooted("publisher", "config", "responses.log"), options.ResponseFilePath);

            var eventOptions = new FileSystemEventClientOptions();
            new Configuration.FileSystem(CreateConfiguration(new()
            {
                [Configuration.FileSystem.OutputRootKey] = TestPaths.Rooted("publisher", "out")
            })).Configure(null, eventOptions);

            Assert.Equal(TestPaths.Rooted("publisher", "out"), eventOptions.OutputFolder);
        }

        [Fact]
        public void FileSystemBlankInitPathUsesPublishedNodesFolderAndDefaultLog()
        {
            var options = new FileSystemRpcServerOptions();
            var configurator = new Configuration.FileSystem(CreateConfiguration(new()
            {
                [PublisherConfig.PublishedNodesFileKey] =
                    TestPaths.Rooted("publisher", "config", "publishednodes.json"),
                [Configuration.FileSystem.InitFilePathKey] = " "
            }));

            configurator.Configure(null, options);

            Assert.Equal(TestPaths.Rooted("publisher", "config", "publishednodes.init"),
                options.RequestFilePath);
            Assert.Equal(TestPaths.Rooted("publisher", "config", "publishednodes.init.log"),
                options.ResponseFilePath);
        }

        [Fact]
        public void MqttConnectionStringConfiguresBrokerOptions()
        {
            var options = new MqttOptions();
            var configurator = new Configuration.MqttBroker(CreateConfiguration(new()
            {
                [Configuration.MqttBroker.MqttClientConnectionStringKey] =
                    "HostName=broker;Port=1884;UserName=user;Password=secret;" +
                    "Protocol=v311;UseTls=true;KeepAlivePeriod=00:00:30;Partitions=4"
            }));

            configurator.Configure(null, options);

            Assert.Equal("broker", options.HostName);
            Assert.Equal(1884, options.Port);
            Assert.Equal("user", options.UserName);
            Assert.Equal("secret", options.Password);
            Assert.Equal(MqttVersion.v311, options.Protocol);
            Assert.Equal(true, options.UseTls);
            Assert.Equal(TimeSpan.FromSeconds(30), options.KeepAlivePeriod);
            Assert.Equal(4, options.NumberOfClientPartitions);
        }

        [Fact]
        public void MqttDiscreteKeysFillUnsetProperties()
        {
            var options = new MqttOptions
            {
                HostName = "existing"
            };
            var configurator = new Configuration.MqttBroker(CreateConfiguration(new()
            {
                [Configuration.MqttBroker.UserNameKey] = "user",
                [Configuration.MqttBroker.PasswordKey] = "secret",
                [Configuration.MqttBroker.HostPortKey] = "1883",
                [Configuration.MqttBroker.ProtocolKey] = "v311",
                [Configuration.MqttBroker.UseTlsKey] = "yes",
                [Configuration.MqttBroker.ClientPartitionsKey] = "2",
                [Configuration.MqttBroker.KeepAlivePeriodKey] = "00:01:00",
                [Configuration.MqttBroker.ClientIdKey] = "client"
            }));

            configurator.Configure(null, options);

            Assert.Equal("existing", options.HostName);
            Assert.Equal("user", options.UserName);
            Assert.Equal("secret", options.Password);
            Assert.Equal(1883, options.Port);
            Assert.Equal(MqttVersion.v311, options.Protocol);
            Assert.Equal(true, options.UseTls);
            Assert.Equal(2, options.NumberOfClientPartitions);
            Assert.Equal(TimeSpan.FromMinutes(1), options.KeepAlivePeriod);
            Assert.Equal("client", options.ClientId);
        }

        [Fact]
        public void ConsoleWriterReadsEnableFlag()
        {
            var options = new ConsoleWriterOptions();
            var configurator = new Configuration.ConsoleWriter(CreateConfiguration(new()
            {
                [Configuration.ConsoleWriter.EnableKey] = "y"
            }));

            configurator.Configure(null, options);

            Assert.Equal(true, options.Enabled);
        }

        [Fact]
        public void IoTEdgeUsesConfiguredConnectionStringAndProduct()
        {
            var options = new IoTEdgeClientOptions();
            var configurator = new Configuration.IoTEdge(CreateConfiguration(new()
            {
                [Configuration.IoTEdge.EdgeHubConnectionString] =
                    "HostName=iot;DeviceId=device;SharedAccessKey=key"
            }));

            configurator.Configure(null, options);

            Assert.Equal("HostName=iot;DeviceId=device;SharedAccessKey=key",
                options.EdgeHubConnectionString);
            Assert.StartsWith("OpcPublisher_", options.Product,
                StringComparison.Ordinal);
        }

        [Fact]
        public void IoTEdgeMarksEdgeEnvironmentWithoutConnectionString()
        {
            using var environment = new EnvironmentVariableScope("IOTEDGE_DEVICEID",
                "device");
            var options = new IoTEdgeClientOptions();

            new Configuration.IoTEdge(CreateConfiguration()).Configure(null, options);

            Assert.Equal(string.Empty, options.EdgeHubConnectionString);
        }

        [Fact]
        public void EventHubsConfiguresConnectionStringAndSchemaRegistry()
        {
            var options = new EventHubsClientOptions();
            var configurator = new Configuration.EventHubs(CreateConfiguration(new()
            {
                [Configuration.EventHubs.EventHubNamespaceConnectionString] =
                    "Endpoint=sb://example.servicebus.windows.net/;SharedAccessKeyName=key",
                [Configuration.EventHubs.SchemaGroupNameKey] = "schemas"
            }));

            configurator.Configure(null, options);

            Assert.Equal(
                "Endpoint=sb://example.servicebus.windows.net/;SharedAccessKeyName=key",
                options.ConnectionString);
            Assert.NotNull(options.SchemaRegistry);
            Assert.Equal(string.Empty,
                options.SchemaRegistry.FullyQualifiedNamespace);
            Assert.Equal("schemas", options.SchemaRegistry.SchemaGroupName);
        }

        [Fact]
        public void RouterKeepsExistingMountPointAndFillsMissingOne()
        {
            var publisher = Options.Create(new PublisherOptions
            {
                PublisherId = "publisher"
            });
            var configurator = new Configuration.Router(CreateConfiguration(), publisher);
            var existing = new RouterOptions
            {
                MountPoint = "custom"
            };
            var missing = new RouterOptions();

            configurator.PostConfigure(null, existing);
            configurator.PostConfigure(null, missing);

            Assert.Equal("custom", existing.MountPoint);
            //
            // The missing one is filled from the topic builder rather than left
            // null. With only a publisher id configured the builder's method
            // topic is empty, so this asserts that it was assigned at all - the
            // value itself is the topic template's business, not the router's.
            //
            Assert.NotNull(missing.MountPoint);
        }

        [Fact]
        public void AioConnectorAppliesDefaultsAndDiscoveryConfiguration()
        {
            using var host = new EnvironmentVariableScope(
                KubernetesEnvironment.ServiceHostEnvironmentVariable, "10.1.2.3");
            using var port = new EnvironmentVariableScope(
                KubernetesEnvironment.ServicePortEnvironmentVariable, "6443");
            using var httpsPort = new EnvironmentVariableScope(
                KubernetesEnvironment.ServicePortHttpsEnvironmentVariable, null);
            var options = new PublisherOptions();
            var configurator = new Configuration.Aio(CreateConfiguration(new()
            {
                [Configuration.Aio.ConnectorId] = "connector",
                [Configuration.Aio.DiscoveredDeviceEndpointTypeKey] = "type",
                [Configuration.Aio.DiscoveredDeviceEndpointTypeVersionKey] = "v1",
                [Configuration.Aio.NetworkDiscoveryModeKey] = "Fast",
                [Configuration.Aio.NetworkDiscoveryIntervalKey] = "12:00:00",
                [Configuration.Aio.NetworkDiscoveryAddressRangesToScanKey] =
                    "10.0.0.0/24",
                [Configuration.Aio.NetworkDiscoveryNetworkProbeTimeoutKey] =
                    "00:00:05",
                [Configuration.Aio.NetworkDiscoveryMaxNetworkProbesKey] = "8",
                [Configuration.Aio.NetworkDiscoveryPortRangesToScanKey] = "4840-4841",
                [Configuration.Aio.NetworkDiscoveryPortProbeTimeoutKey] =
                    "00:00:02",
                [Configuration.Aio.NetworkDiscoveryMaxPortProbesKey] = "16",
                [PublisherConfig.UseFileChangePollingKey] = "true"
            }));

            configurator.Configure(null, options);

            Assert.Equal(true, options.IsAzureIoTOperationsConnector);
            Assert.Equal(true, options.UseStandardsCompliantEncoding);
            Assert.Equal(true, options.EnableCloudEvents);
            Assert.Equal(true, options.EnableRuntimeStateReporting);
            Assert.Equal(PublisherDiagnosticTargetType.Events,
                options.DiagnosticsTarget);
            Assert.Contains(WriterGroupTransport.AioMqtt,
                options.AllowedEventAndDiagnosticsTransports);
            Assert.NotNull(options.SchemaOptions);
            Assert.Equal("status", options.RuntimeStateRoutingInfo);
            Assert.Equal("connector", options.PublisherId);
            Assert.Equal("type", options.AioDiscoveredDeviceEndpointType);
            Assert.Equal("v1", options.AioDiscoveredDeviceEndpointTypeVersion);
            Assert.Equal(DiscoveryMode.Fast, options.AioNetworkDiscoveryMode);
            Assert.Equal(TimeSpan.FromHours(12),
                options.AioNetworkDiscoveryInterval);
            Assert.Equal("10.0.0.0/24",
                options.AioNetworkDiscovery.AddressRangesToScan);
            Assert.Equal(TimeSpan.FromSeconds(5),
                options.AioNetworkDiscovery.NetworkProbeTimeout);
            Assert.Equal(8, options.AioNetworkDiscovery.MaxNetworkProbes);
            Assert.Equal("4840-4841",
                options.AioNetworkDiscovery.PortRangesToScan);
            Assert.Equal(TimeSpan.FromSeconds(2),
                options.AioNetworkDiscovery.PortProbeTimeout);
            Assert.Equal(16, options.AioNetworkDiscovery.MaxPortProbes);
            Assert.Equal(true, options.UseFileChangePolling);
        }

        [Fact]
        public void AioDoesNotChangeOptionsOutsideClusterOrWithoutConnectorId()
        {
            using var host = new EnvironmentVariableScope(
                KubernetesEnvironment.ServiceHostEnvironmentVariable, null);
            var outsideCluster = new PublisherOptions();
            new Configuration.Aio(CreateConfiguration(new()
            {
                [Configuration.Aio.ConnectorId] = "connector"
            })).Configure(null, outsideCluster);

            Assert.Null(outsideCluster.IsAzureIoTOperationsConnector);

            using var inCluster = new EnvironmentVariableScope(
                KubernetesEnvironment.ServiceHostEnvironmentVariable, "10.1.2.3");
            using var port = new EnvironmentVariableScope(
                KubernetesEnvironment.ServicePortEnvironmentVariable, "6443");
            var withoutConnector = new PublisherOptions();

            new Configuration.Aio(CreateConfiguration()).Configure(null, withoutConnector);

            Assert.Null(withoutConnector.IsAzureIoTOperationsConnector);
        }

        private static IConfiguration CreateConfiguration(
            Dictionary<string, string?>? values = null)
        {
            var builder = new ConfigurationBuilder();
            if (values != null)
            {
                builder.AddInMemoryCollection(values);
            }
            return builder.Build();
        }

        private sealed class EnvironmentVariableScope : IDisposable
        {
            public EnvironmentVariableScope(string name, string? value)
            {
                _name = name;
                _previous = Environment.GetEnvironmentVariable(name);
                Environment.SetEnvironmentVariable(name, value);
            }

            public void Dispose()
            {
                Environment.SetEnvironmentVariable(_name, _previous);
            }

            private readonly string _name;
            private readonly string? _previous;
        }
    }

    [CollectionDefinition("EnvironmentVariables", DisableParallelization = true)]
    public sealed class EnvironmentVariablesCollection
    {
    }
}
