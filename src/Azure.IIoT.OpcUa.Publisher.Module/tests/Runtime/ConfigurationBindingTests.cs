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
    using Microsoft.Extensions.Logging.Console;
    using OpenTelemetry.Exporter;
    using OpenTelemetry.Metrics;
    using System;
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
                [Configuration.FileSystem.InitFilePathKey] = TestPaths.Rooted("requests", "requests.http")
            }));

            configurator.Configure(null, options);

            Assert.Equal(TestPaths.Rooted("requests", "requests.http"), options.RequestFilePath);
        }

        // When options.RequestFilePath is already set, the ??= operator skips the config
        // lookup, and the subsequent directory checks also leave it unchanged.
        [Fact]
        public void FileSystemDoesNotOverridePresetRequestFilePath()
        {
            var options = new FileSystemRpcServerOptions
            {
                RequestFilePath = TestPaths.Rooted("preset", "requests.http")
            };
            var configurator = new Configuration.FileSystem(CreateConfiguration(new()
            {
                [Configuration.FileSystem.InitFilePathKey] = TestPaths.Rooted("other", "requests.http")
            }));

            configurator.Configure(null, options);

            Assert.Equal(TestPaths.Rooted("preset", "requests.http"), options.RequestFilePath);
        }

        // When options.ResponseFilePath is already set, the ??= operator skips both
        // the config lookup and the auto-generation from RequestFilePath.
        [Fact]
        public void FileSystemDoesNotOverridePresetResponseFilePath()
        {
            var options = new FileSystemRpcServerOptions
            {
                ResponseFilePath = TestPaths.Rooted("preset", "log.txt")
            };
            var configurator = new Configuration.FileSystem(CreateConfiguration(new()
            {
                [PublisherConfig.PublishedNodesFileKey] =
                    TestPaths.Rooted("publisher", "publishednodes.json"),
                [Configuration.FileSystem.InitFilePathKey] = TestPaths.Rooted("publisher", "requests.http"),
                [Configuration.FileSystem.InitLogFileKey] = TestPaths.Rooted("other", "log.txt")
            }));

            configurator.Configure(null, options);

            Assert.Equal(TestPaths.Rooted("preset", "log.txt"), options.ResponseFilePath);
        }

        // OutputFolder already set → ??= operator skips config lookup.
        [Fact]
        public void FileSystemDoesNotOverridePresetOutputFolder()
        {
            var options = new FileSystemEventClientOptions
            {
                OutputFolder = TestPaths.Rooted("preset", "output")
            };
            var configurator = new Configuration.FileSystem(CreateConfiguration(new()
            {
                [Configuration.FileSystem.OutputRootKey] = TestPaths.Rooted("other", "output")
            }));

            configurator.Configure(null, options);

            Assert.Equal(TestPaths.Rooted("preset", "output"), options.OutputFolder);
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

        // ─── LoggingFormat ────────────────────────────────────────────────────────

        [Fact]
        public void LoggingFormat_SyslogName_SetsSyslogFormatter()
        {
            var options = new ConsoleLoggerOptions();

            new Configuration.LoggingFormat(CreateConfiguration(new()
            {
                [Configuration.LoggingFormat.LogFormatKey] = Syslog.FormatterName
            })).PostConfigure(null, options);

            Assert.Equal(Syslog.FormatterName, options.FormatterName);
        }

        [Fact]
        public void LoggingFormat_Systemd_SetsSystemdFormatter()
        {
            var options = new ConsoleLoggerOptions();

            new Configuration.LoggingFormat(CreateConfiguration(new()
            {
                [Configuration.LoggingFormat.LogFormatKey] = ConsoleFormatterNames.Systemd
            })).PostConfigure(null, options);

            Assert.Equal(ConsoleFormatterNames.Systemd, options.FormatterName);
        }

        [Fact]
        public void LoggingFormat_Simple_SetsSimpleFormatter()
        {
            var options = new ConsoleLoggerOptions();

            new Configuration.LoggingFormat(CreateConfiguration(new()
            {
                [Configuration.LoggingFormat.LogFormatKey] = ConsoleFormatterNames.Simple
            })).PostConfigure(null, options);

            Assert.Equal(ConsoleFormatterNames.Simple, options.FormatterName);
        }

        [Fact]
        public void LoggingFormat_UnknownValue_SetsDefaultFormatter()
        {
            var options = new ConsoleLoggerOptions();

            new Configuration.LoggingFormat(CreateConfiguration(new()
            {
                [Configuration.LoggingFormat.LogFormatKey] = "unknown-format"
            })).PostConfigure(null, options);

            Assert.Equal(Configuration.LoggingFormat.LogFormatDefault, options.FormatterName);
        }

        [Fact]
        public void LoggingFormat_NoKey_SetsDefaultFormatter()
        {
            var options = new ConsoleLoggerOptions();

            new Configuration.LoggingFormat(CreateConfiguration()).PostConfigure(null, options);

            Assert.Equal(Configuration.LoggingFormat.LogFormatDefault, options.FormatterName);
        }

        // ─── ConsoleLogging<T> ────────────────────────────────────────────────────

        [Fact]
        public void ConsoleLogging_Configure_SetsTimestampAndScopes()
        {
            var options = new SimpleConsoleFormatterOptions();

            new Configuration.ConsoleLogging<SimpleConsoleFormatterOptions>(
                CreateConfiguration()).Configure(null, options);

            Assert.Equal("[yy-MM-dd HH:mm:ss.ffff] ", options.TimestampFormat);
            Assert.True(options.IncludeScopes);
            Assert.True(options.UseUtcTimestamp);
        }

        [Fact]
        public void ConsoleLogging_ConfigureNoName_SetsTimestampAndScopes()
        {
            var options = new SimpleConsoleFormatterOptions();

            new Configuration.ConsoleLogging<SimpleConsoleFormatterOptions>(
                CreateConfiguration()).Configure(options);

            Assert.Equal("[yy-MM-dd HH:mm:ss.ffff] ", options.TimestampFormat);
            Assert.True(options.IncludeScopes);
        }

        // ─── Otel.Configure (name-based endpoint selection) ──────────────────────

        [Fact]
        public void OtelConfigureWithMetricsNameSetsEndpoint()
        {
            var configurator = new Configuration.Otel(CreateConfiguration(new()
            {
                [Configuration.Otel.OtlpCollectorEndpointKey] = "http://metrics-collector:4317"
            }));
            var exporter = new OtlpExporterOptions();

            configurator.Configure("Metrics", exporter);

            Assert.Equal("http://metrics-collector:4317/", exporter.Endpoint.ToString());
        }

        [Fact]
        public void OtelConfigureWithTracesNameSetsEndpoint()
        {
            var configurator = new Configuration.Otel(CreateConfiguration(new()
            {
                [Configuration.Otel.OtlpCollectorEndpointKey] = "http://traces-collector:4317"
            }));
            var exporter = new OtlpExporterOptions();

            configurator.Configure("Traces", exporter);

            Assert.Equal("http://traces-collector:4317/", exporter.Endpoint.ToString());
        }

        [Fact]
        public void OtelConfigureWithLoggingNameSetsEndpoint()
        {
            var configurator = new Configuration.Otel(CreateConfiguration(new()
            {
                [Configuration.Otel.OtlpCollectorEndpointKey] = "http://log-collector:4317"
            }));
            var exporter = new OtlpExporterOptions();

            configurator.Configure("Logging", exporter);

            Assert.Equal("http://log-collector:4317/", exporter.Endpoint.ToString());
        }

        [Fact]
        public void OtelEnableMetricsTrueWithEndpoint_EnablesOtelMetrics()
        {
            var configurator = new Configuration.Otel(CreateConfiguration(new()
            {
                [Configuration.Otel.EnableMetricsKey] = "true",
                [Configuration.Otel.OtlpCollectorEndpointKey] = "http://collector:4317"
            }));

            Assert.True(configurator.EnableOtelMetrics);
            Assert.False(configurator.AddPrometheusEndpoint);
        }

        [Fact]
        public void OtelEnableMetricsTrueNoEndpoint_EnablesPrometheus()
        {
            var configurator = new Configuration.Otel(CreateConfiguration(new()
            {
                [Configuration.Otel.EnableMetricsKey] = "true"
            }));

            Assert.True(configurator.AddPrometheusEndpoint);
            Assert.False(configurator.EnableOtelMetrics);
        }

        [Fact]
        public void OtelWithTracesEndpoint_EnablesTraces()
        {
            var configurator = new Configuration.Otel(CreateConfiguration(new()
            {
                [Configuration.Otel.EnableOtelTracesKey] = "true",
                [Configuration.Otel.OtlpCollectorEndpointKey] = "http://collector:4317"
            }));

            Assert.True(configurator.EnableOtelTraces);
        }

        [Fact]
        public void OtelMaxMetricStreams_UsesConfiguredValue()
        {
            var configurator = new Configuration.Otel(CreateConfiguration(new()
            {
                [Configuration.Otel.OtlpMaxMetricStreamsKey] = "2000"
            }));

            Assert.Equal(2000, configurator.MaxMetricStreams);
        }

        [Fact]
        public void OtelMaxMetricStreams_DefaultsToOtlpMax()
        {
            var configurator = new Configuration.Otel(CreateConfiguration());

            Assert.Equal(Configuration.Otel.OtlpMaxMetricDefault, configurator.MaxMetricStreams);
        }

        [Fact]
        public void OtelRuntimeInstrumentation_DefaultIsFalse()
        {
            var configurator = new Configuration.Otel(CreateConfiguration());

            Assert.Equal(Configuration.Otel.OtlpRuntimeInstrumentationDefault,
                configurator.AddRuntimeInstrumentation);
        }

        [Fact]
        public void OtelTotalNameSuffix_DefaultIsFalse()
        {
            var configurator = new Configuration.Otel(CreateConfiguration());

            Assert.Equal(Configuration.Otel.OtlpTotalNameSuffixForCountersDefault,
                configurator.EnableTotalNameSuffixForCounters);
        }

        // ─── Otel.Configure MetricReaderOptions ──────────────────────────────────

        [Fact]
        public void OtelConfigureMetricReaderOptions_UsesDefaultInterval()
        {
            var configurator = new Configuration.Otel(CreateConfiguration());
            var options = new MetricReaderOptions();

            configurator.Configure(null, options);

            Assert.Equal(
                Configuration.Otel.OtlpExportIntervalMillisecondsDefault,
                options.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds);
        }

        [Fact]
        public void OtelConfigureMetricReaderOptions_UsesConfiguredInterval()
        {
            var configurator = new Configuration.Otel(CreateConfiguration(new()
            {
                [Configuration.Otel.OtlpExportIntervalMillisecondsKey] = "5000"
            }));
            var options = new MetricReaderOptions();

            configurator.Configure(null, options);

            Assert.Equal(5000, options.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds);
        }

        // ─── LoggingLevel serilog aliases ─────────────────────────────────────────

        [Fact]
        public void LoggingLevelVerboseAliasSetsTrace()
        {
            var options = new LoggerFilterOptions { MinLevel = LogLevel.Information };

            new Configuration.LoggingLevel(CreateConfiguration(new()
            {
                [Configuration.LoggingLevel.LogLevelKey] = "Verbose"
            })).Configure(null, options);

            Assert.Equal(LogLevel.Trace, options.MinLevel);
        }

        [Fact]
        public void LoggingLevelFatalAliasSetsLogLevelCritical()
        {
            var options = new LoggerFilterOptions { MinLevel = LogLevel.Information };

            new Configuration.LoggingLevel(CreateConfiguration(new()
            {
                [Configuration.LoggingLevel.LogLevelKey] = "Fatal"
            })).Configure(null, options);

            Assert.Equal(LogLevel.Critical, options.MinLevel);
        }

        // ─── LoggingLevel logs:level aliases ─────────────────────────────────────

        [Fact]
        public void LoggingLevelDiagnosticsTraceSetsLogLevelTrace()
        {
            var options = new LoggerFilterOptions { MinLevel = LogLevel.Information };

            new Configuration.LoggingLevel(CreateConfiguration(new()
            {
                ["logs:level"] = "trace"
            })).Configure(null, options);

            Assert.Equal(LogLevel.Trace, options.MinLevel);
        }

        [Fact]
        public void LoggingLevelDiagnosticsDebugSetsDebug()
        {
            var options = new LoggerFilterOptions { MinLevel = LogLevel.Information };

            new Configuration.LoggingLevel(CreateConfiguration(new()
            {
                ["logs:level"] = "debug"
            })).Configure(null, options);

            Assert.Equal(LogLevel.Debug, options.MinLevel);
        }

        [Fact]
        public void LoggingLevelDiagnosticsInfoSetsInformation()
        {
            var options = new LoggerFilterOptions { MinLevel = LogLevel.Warning };

            new Configuration.LoggingLevel(CreateConfiguration(new()
            {
                ["logs:level"] = "info"
            })).Configure(null, options);

            Assert.Equal(LogLevel.Information, options.MinLevel);
        }

        [Fact]
        public void LoggingLevelDiagnosticsWarnSetsWarning()
        {
            var options = new LoggerFilterOptions { MinLevel = LogLevel.Information };

            new Configuration.LoggingLevel(CreateConfiguration(new()
            {
                ["logs:level"] = "warn"
            })).Configure(null, options);

            Assert.Equal(LogLevel.Warning, options.MinLevel);
        }

        [Fact]
        public void LoggingLevelDiagnosticsErrorSetsError()
        {
            var options = new LoggerFilterOptions { MinLevel = LogLevel.Information };

            new Configuration.LoggingLevel(CreateConfiguration(new()
            {
                ["logs:level"] = "error"
            })).Configure(null, options);

            Assert.Equal(LogLevel.Error, options.MinLevel);
        }

        [Fact]
        public void LoggingLevelDiagnosticsNoneSetsNone()
        {
            var options = new LoggerFilterOptions { MinLevel = LogLevel.Information };

            new Configuration.LoggingLevel(CreateConfiguration(new()
            {
                ["logs:level"] = "none"
            })).Configure(null, options);

            Assert.Equal(LogLevel.None, options.MinLevel);
        }

        [Fact]
        public void LoggingLevelDiagnosticsValidEnumSetsValue()
        {
            var options = new LoggerFilterOptions { MinLevel = LogLevel.Information };

            new Configuration.LoggingLevel(CreateConfiguration(new()
            {
                ["logs:level"] = "critical"
            })).Configure(null, options);

            Assert.Equal(LogLevel.Critical, options.MinLevel);
        }

        // ─── Dapr connection string additional paths ──────────────────────────────

        [Fact]
        public void DaprConnectionStringWithStateStoreSetsStateStoreName()
        {
            var options = new DaprOptions();
            var configurator = new Configuration.Dapr(CreateConfiguration(new()
            {
                [Configuration.Dapr.DaprConnectionStringKey] =
                    $"StateStore=mystore"
            }));

            configurator.Configure(null, options);

            Assert.Equal("mystore", options.StateStoreName);
        }

        [Fact]
        public void DaprConnectionStringWithGrpcPortSetsGrpcEndpoint()
        {
            var options = new DaprOptions();
            var configurator = new Configuration.Dapr(CreateConfiguration(new()
            {
                [Configuration.Dapr.DaprConnectionStringKey] =
                    "PubSubComponent=pub;GrpcPort=50001"
            }));

            configurator.Configure(null, options);

            Assert.Equal("http://localhost:50001", options.GrpcEndpoint);
        }

        [Fact]
        public void DaprConnectionStringWithHttpPortSetsHttpEndpoint()
        {
            var options = new DaprOptions();
            var configurator = new Configuration.Dapr(CreateConfiguration(new()
            {
                [Configuration.Dapr.DaprConnectionStringKey] =
                    "PubSubComponent=pub;HttpPort=3500"
            }));

            configurator.Configure(null, options);

            Assert.Equal("http://localhost:3500", options.HttpEndpoint);
        }

        [Fact]
        public void DaprConnectionStringWithCheckSideCarHealthTrue_SetsFlag()
        {
            var options = new DaprOptions();
            var configurator = new Configuration.Dapr(CreateConfiguration(new()
            {
                [Configuration.Dapr.DaprConnectionStringKey] =
                    "PubSubComponent=pub;CheckSideCarHealth=true"
            }));

            configurator.Configure(null, options);

            Assert.True(options.CheckSideCarHealthBeforeAccess);
        }

        [Fact]
        public void DaprConnectionStringWithSchemeAndHost_SetsCustomEndpoint()
        {
            var options = new DaprOptions();
            var configurator = new Configuration.Dapr(CreateConfiguration(new()
            {
                [Configuration.Dapr.DaprConnectionStringKey] =
                    "PubSubComponent=pub;Scheme=https;Host=dapr-sidecar;GrpcPort=50001"
            }));

            configurator.Configure(null, options);

            Assert.Equal("https://dapr-sidecar:50001", options.GrpcEndpoint);
        }

        [Fact]
        public void DaprNoConnectionStringWithStateStoreKey_SetsStateStoreName()
        {
            var options = new DaprOptions();

            new Configuration.Dapr(CreateConfiguration(new()
            {
                [Configuration.Dapr.StateStoreKey] = "my-state"
            })).Configure(null, options);

            Assert.Equal("my-state", options.StateStoreName);
        }

        [Fact]
        public void DaprApiToken_SetFromKey()
        {
            var options = new DaprOptions();

            new Configuration.Dapr(CreateConfiguration(new()
            {
                [Configuration.Dapr.PubSubComponentKey] = "pub",
                ["DAPR_API_TOKEN"] = "mytoken"
            })).Configure(null, options);

            Assert.Equal("mytoken", options.ApiToken);
        }

        // ─── Http additional paths ────────────────────────────────────────────────

        [Fact]
        public void HttpConnectionStringWithNumericPortSetsPort()
        {
            var options = new HttpEventClientOptions();
            var configurator = new Configuration.Http(CreateConfiguration(new()
            {
                [Configuration.Http.HttpConnectionStringKey] = "HostName=hook;Port=8080"
            }));

            configurator.Configure(null, options);

            Assert.Equal(8080, options.Port);
        }

        [Fact]
        public void HttpConnectionStringWithHttpSchemeSetsHttpFlag()
        {
            var options = new HttpEventClientOptions();
            var configurator = new Configuration.Http(CreateConfiguration(new()
            {
                [Configuration.Http.HttpConnectionStringKey] = "HostName=hook;Scheme=http"
            }));

            configurator.Configure(null, options);

            Assert.Equal(true, options.UseHttpScheme);
        }

        [Fact]
        public void HttpConnectionStringWithPutKeySetsHttpPutFlag()
        {
            var options = new HttpEventClientOptions();
            var configurator = new Configuration.Http(CreateConfiguration(new()
            {
                [Configuration.Http.HttpConnectionStringKey] = "HostName=hook;Put=true"
            }));

            configurator.Configure(null, options);

            Assert.Equal(true, options.UseHttpPutMethod);
        }

        [Fact]
        public void HttpAbsoluteWebhookUrl_SetsHostNameAndPort()
        {
            var options = new HttpEventClientOptions();
            var configurator = new Configuration.Http(CreateConfiguration(new()
            {
                [Configuration.Http.WebHookHostUrlKey] = "http://webhook.example.com:9000/hook"
            }));

            configurator.Configure(null, options);

            Assert.Equal("webhook.example.com", options.HostName);
            Assert.Equal(9000, options.Port);
            Assert.Equal(true, options.UseHttpScheme);
        }

        // ─── MqttBroker additional paths ──────────────────────────────────────────

        [Fact]
        public void MqttBrokerConnectionStringWithUserNameAndPassword_SetsCredentials()
        {
            var options = new MqttOptions();
            var configurator = new Configuration.MqttBroker(CreateConfiguration(new()
            {
                [Configuration.MqttBroker.MqttClientConnectionStringKey] =
                    "HostName=broker;UserName=user;Password=pass"
            }));

            configurator.Configure(null, options);

            Assert.Equal("user", options.UserName);
            Assert.Equal("pass", options.Password);
        }

        [Fact]
        public void MqttBrokerConnectionStringWithProtocol_SetsVersion()
        {
            var options = new MqttOptions();
            var configurator = new Configuration.MqttBroker(CreateConfiguration(new()
            {
                [Configuration.MqttBroker.MqttClientConnectionStringKey] =
                    "HostName=broker;Protocol=v5"
            }));

            configurator.Configure(null, options);

            Assert.Equal(MqttVersion.v5, options.Protocol);
        }

        [Fact]
        public void MqttBrokerConnectionStringWithUseTls_SetsFlag()
        {
            var options = new MqttOptions();
            var configurator = new Configuration.MqttBroker(CreateConfiguration(new()
            {
                [Configuration.MqttBroker.MqttClientConnectionStringKey] =
                    "HostName=broker;UseTls=true"
            }));

            configurator.Configure(null, options);

            Assert.Equal(true, options.UseTls);
        }

        [Fact]
        public void MqttBrokerConnectionStringWithPartitions_SetsCount()
        {
            var options = new MqttOptions();
            var configurator = new Configuration.MqttBroker(CreateConfiguration(new()
            {
                [Configuration.MqttBroker.MqttClientConnectionStringKey] =
                    "HostName=broker;Partitions=4"
            }));

            configurator.Configure(null, options);

            Assert.Equal(4, options.NumberOfClientPartitions);
        }

        [Fact]
        public void MqttBrokerConnectionStringWithKeepAlive_SetsKeepAlivePeriod()
        {
            var options = new MqttOptions();
            var configurator = new Configuration.MqttBroker(CreateConfiguration(new()
            {
                [Configuration.MqttBroker.MqttClientConnectionStringKey] =
                    "HostName=broker;KeepAlivePeriod=00:00:30"
            }));

            configurator.Configure(null, options);

            Assert.Equal(TimeSpan.FromSeconds(30), options.KeepAlivePeriod);
        }

        [Fact]
        public void MqttBrokerDiscreteKeys_SetHostNameAndUserAndProtocol()
        {
            var options = new MqttOptions();
            var configurator = new Configuration.MqttBroker(CreateConfiguration(new()
            {
                [Configuration.MqttBroker.HostNameKey] = "mybroker",
                [Configuration.MqttBroker.UserNameKey] = "user",
                [Configuration.MqttBroker.PasswordKey] = "pass",
                [Configuration.MqttBroker.ProtocolKey] = "v311",
                [Configuration.MqttBroker.UseTlsKey] = "true",
                [Configuration.MqttBroker.HostPortKey] = "1884",
                [Configuration.MqttBroker.ClientPartitionsKey] = "2"
            }));

            configurator.Configure(null, options);

            Assert.Equal("mybroker", options.HostName);
            Assert.Equal("user", options.UserName);
            Assert.Equal("pass", options.Password);
            Assert.Equal(MqttVersion.v311, options.Protocol);
            Assert.Equal(true, options.UseTls);
            Assert.Equal(1884, options.Port);
            Assert.Equal(2, options.NumberOfClientPartitions);
        }

        [Fact]
        public void MqttBrokerDiscreteKeepAlivePeriod_SetsDuration()
        {
            var options = new MqttOptions();
            var configurator = new Configuration.MqttBroker(CreateConfiguration(new()
            {
                [Configuration.MqttBroker.HostNameKey] = "broker",
                [Configuration.MqttBroker.KeepAlivePeriodKey] = "00:01:00"
            }));

            configurator.Configure(null, options);

            Assert.Equal(TimeSpan.FromMinutes(1), options.KeepAlivePeriod);
        }

        // ─── EventHubs SchemaGroupName path ──────────────────────────────────────

        [Fact]
        public void EventHubsWithSchemaGroupNameSetsSchemaRegistry()
        {
            var options = new EventHubsClientOptions();
            var configurator = new Configuration.EventHubs(CreateConfiguration(new()
            {
                [Configuration.EventHubs.EventHubNamespaceConnectionString] =
                    "Endpoint=sb://ns.servicebus.windows.net/;SharedAccessKeyName=k",
                [Configuration.EventHubs.SchemaGroupNameKey] = "my-schema-group"
            }));

            configurator.Configure(null, options);

            Assert.NotNull(options.SchemaRegistry);
            Assert.Equal("my-schema-group", options.SchemaRegistry.SchemaGroupName);
        }

        // ─── FileSystem blank/just-filename paths ─────────────────────────────────

        [Fact]
        public void FileSystemInitFilePathBlank_UsesPublishedNodesFolder()
        {
            var options = new FileSystemRpcServerOptions();
            var configurator = new Configuration.FileSystem(CreateConfiguration(new()
            {
                [Configuration.FileSystem.InitFilePathKey] = "   " // blank
            }));

            configurator.Configure(null, options);

            Assert.NotNull(options.RequestFilePath);
            Assert.EndsWith("publishednodes.init", options.RequestFilePath,
                StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void FileSystemInitFilePathJustFilename_CombinesWithRootFolder()
        {
            var options = new FileSystemRpcServerOptions();
            var configurator = new Configuration.FileSystem(CreateConfiguration(new()
            {
                [Configuration.FileSystem.InitFilePathKey] = "requests.http"
            }));

            configurator.Configure(null, options);

            Assert.NotNull(options.RequestFilePath);
            Assert.EndsWith("requests.http", options.RequestFilePath,
                StringComparison.OrdinalIgnoreCase);
            // Should have a directory component (path was combined with rootFolder)
            Assert.NotNull(System.IO.Path.GetDirectoryName(options.RequestFilePath));
            Assert.True(System.IO.Path.GetDirectoryName(options.RequestFilePath)!.Length > 0);
        }

        [Fact]
        public void FileSystemWithInitLogFileJustFilename_CombinesWithRequestFolder()
        {
            var options = new FileSystemRpcServerOptions();
            var configurator = new Configuration.FileSystem(CreateConfiguration(new()
            {
                [Configuration.FileSystem.InitFilePathKey] = TestPaths.Rooted("publisher", "requests.http"),
                [Configuration.FileSystem.InitLogFileKey] = "mylog.txt"
            }));

            configurator.Configure(null, options);

            Assert.NotNull(options.ResponseFilePath);
            Assert.EndsWith("mylog.txt", options.ResponseFilePath,
                StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void FileSystemWithNoLogFileKey_UsesRequestFileWithLogSuffix()
        {
            var options = new FileSystemRpcServerOptions();
            var configurator = new Configuration.FileSystem(CreateConfiguration(new()
            {
                [Configuration.FileSystem.InitFilePathKey] = TestPaths.Rooted("publisher", "requests.http")
            }));

            configurator.Configure(null, options);

            Assert.Equal(TestPaths.Rooted("publisher", "requests.http.log"), options.ResponseFilePath);
        }

        // ─── IoTEdge with IOTEDGE_DEVICEID env var ────────────────────────────────

        [Fact]
        public void IoTEdgeWithIotEdgeDeviceIdEnvVar_SetsEmptyConnectionString()
        {
            var savedId = Environment.GetEnvironmentVariable("IOTEDGE_DEVICEID");
            try
            {
                Environment.SetEnvironmentVariable("IOTEDGE_DEVICEID", "test-device");
                var options = new IoTEdgeClientOptions();

                new Configuration.IoTEdge(CreateConfiguration()).Configure(null, options);

                Assert.Equal(string.Empty, options.EdgeHubConnectionString);
            }
            finally
            {
                Environment.SetEnvironmentVariable("IOTEDGE_DEVICEID", savedId);
            }
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

