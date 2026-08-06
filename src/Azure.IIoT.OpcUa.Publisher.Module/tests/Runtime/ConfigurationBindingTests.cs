// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Runtime
{
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Module.Runtime;
    using Azure.IIoT.OpcUa.Core.Messaging.Clients;
    using Azure.IIoT.OpcUa.Core.Messaging.Clients.Dapr;
    using Azure.IIoT.OpcUa.Core.Messaging.Clients.EventHubs;
    using Azure.IIoT.OpcUa.Core.Messaging.Clients.IoTEdge;
    using Azure.IIoT.OpcUa.Core.Messaging.Clients.Mqtt;
    using Azure.IIoT.OpcUa.Core.Rpc.Servers;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using OpenTelemetry.Exporter;
    using System.Collections.Generic;
    using Xunit;

    /// <summary>
    /// Tests for the inner Configuration configurators that target branches and
    /// execution paths not already covered by ConfigurationTests.
    /// </summary>
    public sealed class ConfigurationBindingTests
    {
        // ─── Otel ───────────────────────────────────────────────────────────────

        // When the exporter-name argument is null (or any unknown value), the switch
        // arm returns null → condition !IsNullOrEmpty(null) is false → Endpoint unchanged.
        [Fact]
        public void OtelWithNullExporterNameDoesNotModifyEndpoint()
        {
            var configurator = new Configuration.Otel(CreateConfiguration(new()
            {
                [Configuration.Otel.OtlpCollectorEndpointKey] = "http://collector:4317"
            }));
            var exporter = new OtlpExporterOptions();
            var initial = exporter.Endpoint;

            configurator.Configure(null, exporter);

            Assert.Equal(initial, exporter.Endpoint);
        }

        // EnableMetrics=false → both AddPrometheusEndpoint and EnableOtelMetrics are false
        // (tests the short-circuit false branch of each computed property)
        [Fact]
        public void OtelMetricsDisabledByFlagReportsNeitherPrometheusNorOtel()
        {
            var configurator = new Configuration.Otel(CreateConfiguration(new()
            {
                [Configuration.Otel.EnableMetricsKey] = "false"
            }));

            Assert.Equal(false, configurator.AddPrometheusEndpoint);
            Assert.Equal(false, configurator.EnableOtelMetrics);
        }

        // EnableOtelLogging=true but no endpoint configured → false because the second
        // operand of && is false (empty endpoint string).
        [Fact]
        public void OtelLoggingRemainsDisabledWhenKeyTrueButNoEndpointConfigured()
        {
            var configurator = new Configuration.Otel(CreateConfiguration(new()
            {
                [Configuration.Otel.EnableOtelLoggingKey] = "true"
            }));

            Assert.Equal(false, configurator.EnableOtelLogging);
        }

        // ─── LoggingLevel ────────────────────────────────────────────────────────

        // Neither LogLevel nor logs:level key present → method does nothing → MinLevel unchanged.
        [Fact]
        public void LoggingLevelMakesNoChangeWhenNoKeysConfigured()
        {
            var options = new LoggerFilterOptions { MinLevel = LogLevel.Information };

            new Configuration.LoggingLevel(CreateConfiguration()).Configure(null, options);

            Assert.Equal(LogLevel.Information, options.MinLevel);
        }

        // LogLevel key contains a string that is neither a valid LogLevel enum value nor
        // a Serilog alias ("Verbose"/"Fatal") → the else-block switch falls through without
        // setting MinLevel → method returns without change.
        [Fact]
        public void LoggingLevelMakesNoChangeForUnrecognisedCommandLineKey()
        {
            var options = new LoggerFilterOptions { MinLevel = LogLevel.Information };

            new Configuration.LoggingLevel(CreateConfiguration(new()
            {
                [Configuration.LoggingLevel.LogLevelKey] = "garbage"
            })).Configure(null, options);

            Assert.Equal(LogLevel.Information, options.MinLevel);
        }

        // logs:level with a completely unrecognisable value → the final Enum.TryParse
        // also fails → MinLevel unchanged.
        [Fact]
        public void LoggingLevelDiagnosticsGarbageValueMakesNoChange()
        {
            var options = new LoggerFilterOptions { MinLevel = LogLevel.Information };

            new Configuration.LoggingLevel(CreateConfiguration(new()
            {
                ["logs:level"] = "garbage"
            })).Configure(null, options);

            Assert.Equal(LogLevel.Information, options.MinLevel);
        }

        // ─── Dapr ────────────────────────────────────────────────────────────────

        // Minimal connection string (PubSubComponent only): GrpcPort/HttpPort are absent
        // → int.TryParse is not called → GrpcEndpoint and HttpEndpoint remain null.
        // CheckSideCarHealth absent → CheckSideCarHealthBeforeAccess stays false.
        [Fact]
        public void DaprMinimalConnectionStringDoesNotSetGrpcOrHttpEndpoints()
        {
            var options = new DaprOptions();
            var configurator = new Configuration.Dapr(CreateConfiguration(new()
            {
                [Configuration.Dapr.DaprConnectionStringKey] = "PubSubComponent=pub"
            }));

            configurator.Configure(null, options);

            Assert.Equal("pub", options.PubSubComponent);
            Assert.Null(options.GrpcEndpoint);
            Assert.Null(options.HttpEndpoint);
            Assert.Equal(false, options.CheckSideCarHealthBeforeAccess);
        }

        // No DaprConnectionString, no DAPR_API_TOKEN in configuration → ApiToken stays null.
        [Fact]
        public void DaprNoConnectionStringWithoutApiTokenKeyLeavesTokenNull()
        {
            var options = new DaprOptions();

            new Configuration.Dapr(CreateConfiguration(new()
            {
                [Configuration.Dapr.PubSubComponentKey] = "pub"
            })).Configure(null, options);

            Assert.Null(options.ApiToken);
        }

        // ─── Http ────────────────────────────────────────────────────────────────

        // Scheme=https → the value.Equals("http", ...) check is false → UseHttpScheme stays false.
        [Fact]
        public void HttpConnectionStringWithHttpsSchemeDoesNotSetHttpSchemeFlag()
        {
            var options = new HttpEventClientOptions();
            var configurator = new Configuration.Http(CreateConfiguration(new()
            {
                [Configuration.Http.HttpConnectionStringKey] = "HostName=hook;Scheme=https"
            }));

            configurator.Configure(null, options);

            Assert.Equal("hook", options.HostName);
            Assert.Null(options.UseHttpScheme);
        }

        // No Put key in connection string → TryGetValue fails → UseHttpPutMethod stays false.
        [Fact]
        public void HttpConnectionStringWithoutPutKeyLeavesMethodFlagFalse()
        {
            var options = new HttpEventClientOptions();
            var configurator = new Configuration.Http(CreateConfiguration(new()
            {
                [Configuration.Http.HttpConnectionStringKey] = "HostName=hook"
            }));

            configurator.Configure(null, options);

            Assert.Null(options.UseHttpPutMethod);
        }

        // No connection string and no webhook URL → Configure returns without setting HostName.
        [Fact]
        public void HttpEmptyConfigurationLeavesHostNameNull()
        {
            var options = new HttpEventClientOptions();

            new Configuration.Http(CreateConfiguration()).Configure(null, options);

            Assert.Null(options.HostName);
        }

        // A relative (non-absolute) webhook URL fails Uri.TryCreate(UriKind.Absolute) →
        // HostName not set.
        [Fact]
        public void HttpRelativeWebhookUrlDoesNotSetHostName()
        {
            var options = new HttpEventClientOptions();
            var configurator = new Configuration.Http(CreateConfiguration(new()
            {
                [Configuration.Http.WebHookHostUrlKey] = "not-absolute-url"
            }));

            configurator.Configure(null, options);

            Assert.Null(options.HostName);
        }

        // ─── FileSystem ──────────────────────────────────────────────────────────

        // No InitFilePath key → GetStringOrDefault returns null → options.RequestFilePath
        // stays null → method returns early.
        [Fact]
        public void FileSystemWithNoInitFilePathLeavesRequestFilePathNull()
        {
            var options = new FileSystemRpcServerOptions();

            new Configuration.FileSystem(CreateConfiguration()).Configure(null, options);

            Assert.Null(options.RequestFilePath);
        }

        // InitFilePath with a full absolute path has a non-empty directory component →
        // neither the blank-placeholder branch nor the "just a filename" branch fires →
        // the path is kept as-is.
        [Fact]
        public void FileSystemAbsoluteInitFilePathIsKeptUnchanged()
        {
            var options = new FileSystemRpcServerOptions();
            var configurator = new Configuration.FileSystem(CreateConfiguration(new()
            {
                [Configuration.FileSystem.InitFilePathKey] = @"C:\requests\requests.http"
            }));

            configurator.Configure(null, options);

            Assert.Equal(@"C:\requests\requests.http", options.RequestFilePath);
        }

        // When options.RequestFilePath is already set, the ??= operator skips the config
        // lookup, and the subsequent directory checks also leave it unchanged.
        [Fact]
        public void FileSystemDoesNotOverridePresetRequestFilePath()
        {
            var options = new FileSystemRpcServerOptions
            {
                RequestFilePath = @"C:\preset\requests.http"
            };
            var configurator = new Configuration.FileSystem(CreateConfiguration(new()
            {
                [Configuration.FileSystem.InitFilePathKey] = @"C:\other\requests.http"
            }));

            configurator.Configure(null, options);

            Assert.Equal(@"C:\preset\requests.http", options.RequestFilePath);
        }

        // When options.ResponseFilePath is already set, the ??= operator skips both
        // the config lookup and the auto-generation from RequestFilePath.
        [Fact]
        public void FileSystemDoesNotOverridePresetResponseFilePath()
        {
            var options = new FileSystemRpcServerOptions
            {
                ResponseFilePath = @"C:\preset\log.txt"
            };
            var configurator = new Configuration.FileSystem(CreateConfiguration(new()
            {
                [PublisherConfig.PublishedNodesFileKey] =
                    @"C:\publisher\publishednodes.json",
                [Configuration.FileSystem.InitFilePathKey] = @"C:\publisher\requests.http",
                [Configuration.FileSystem.InitLogFileKey] = @"C:\other\log.txt"
            }));

            configurator.Configure(null, options);

            Assert.Equal(@"C:\preset\log.txt", options.ResponseFilePath);
        }

        // OutputFolder already set → ??= operator skips config lookup.
        [Fact]
        public void FileSystemDoesNotOverridePresetOutputFolder()
        {
            var options = new FileSystemEventClientOptions
            {
                OutputFolder = @"C:\preset\output"
            };
            var configurator = new Configuration.FileSystem(CreateConfiguration(new()
            {
                [Configuration.FileSystem.OutputRootKey] = @"C:\other\output"
            }));

            configurator.Configure(null, options);

            Assert.Equal(@"C:\preset\output", options.OutputFolder);
        }

        // ─── MqttBroker ──────────────────────────────────────────────────────────

        // Port value in connection string is not numeric → int.TryParse fails → Port not set.
        [Fact]
        public void MqttBrokerConnectionStringWithNonNumericPortDoesNotSetPort()
        {
            var options = new MqttOptions();
            var configurator = new Configuration.MqttBroker(CreateConfiguration(new()
            {
                [Configuration.MqttBroker.MqttClientConnectionStringKey] =
                    "HostName=broker;Port=notanumber"
            }));

            configurator.Configure(null, options);

            Assert.Equal("broker", options.HostName);
            Assert.Null(options.Port);
        }

        // ClientId preset on options → the final "if (string.IsNullOrEmpty(options.ClientId))"
        // guard is false → discrete key is ignored.
        [Fact]
        public void MqttBrokerPresetClientIdNotOverriddenByDiscreteKey()
        {
            var options = new MqttOptions { ClientId = "preset-client" };
            var configurator = new Configuration.MqttBroker(CreateConfiguration(new()
            {
                [Configuration.MqttBroker.ClientIdKey] = "other-client"
            }));

            configurator.Configure(null, options);

            Assert.Equal("preset-client", options.ClientId);
        }

        // ─── IoTEdge ─────────────────────────────────────────────────────────────

        // EdgeHubConnectionString already set → the IsNullOrEmpty guard is false →
        // config lookup is skipped.
        [Fact]
        public void IoTEdgePresetConnectionStringNotOverriddenByConfig()
        {
            var options = new IoTEdgeClientOptions
            {
                EdgeHubConnectionString = "HostName=preset;DeviceId=d;SharedAccessKey=k"
            };
            new Configuration.IoTEdge(CreateConfiguration(new()
            {
                [Configuration.IoTEdge.EdgeHubConnectionString] =
                    "HostName=other;DeviceId=d;SharedAccessKey=k"
            })).Configure(null, options);

            Assert.Equal(
                "HostName=preset;DeviceId=d;SharedAccessKey=k",
                options.EdgeHubConnectionString);
        }

        // ─── EventHubs ───────────────────────────────────────────────────────────

        // ConnectionString already set → IsNullOrEmpty guard is false → config key skipped.
        [Fact]
        public void EventHubsPresetConnectionStringNotOverriddenByConfig()
        {
            var options = new EventHubsClientOptions
            {
                ConnectionString = "Endpoint=sb://preset.servicebus.windows.net/;SharedAccessKeyName=k"
            };
            new Configuration.EventHubs(CreateConfiguration(new()
            {
                [Configuration.EventHubs.EventHubNamespaceConnectionString] =
                    "Endpoint=sb://other.servicebus.windows.net/;SharedAccessKeyName=k"
            })).Configure(null, options);

            Assert.Equal(
                "Endpoint=sb://preset.servicebus.windows.net/;SharedAccessKeyName=k",
                options.ConnectionString);
        }

        // No SchemaGroupName configured → IsNullOrEmpty check is true → SchemaRegistry not set.
        [Fact]
        public void EventHubsWithNoSchemaGroupNameDoesNotSetSchemaRegistry()
        {
            var options = new EventHubsClientOptions();
            var configurator = new Configuration.EventHubs(CreateConfiguration(new()
            {
                [Configuration.EventHubs.EventHubNamespaceConnectionString] =
                    "Endpoint=sb://ns.servicebus.windows.net/;SharedAccessKeyName=k"
            }));

            configurator.Configure(null, options);

            Assert.NotNull(options.ConnectionString);
            Assert.Null(options.SchemaRegistry);
        }

        // ─── ConsoleWriter ────────────────────────────────────────────────────────

        // No EnableConsoleWriter key → GetBoolOrDefault returns false → Enabled = false.
        [Fact]
        public void ConsoleWriterWithNoKeyLeavesEnabledFalse()
        {
            var options = new ConsoleWriterOptions();

            new Configuration.ConsoleWriter(CreateConfiguration()).Configure(null, options);

            Assert.Equal(false, options.Enabled);
        }

        // ─── helpers ─────────────────────────────────────────────────────────────

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
    }
}
