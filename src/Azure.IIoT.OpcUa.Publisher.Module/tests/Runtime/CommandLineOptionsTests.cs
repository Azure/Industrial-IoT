// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Runtime
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Module.Runtime;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Runtime;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Globalization;
    using Xunit;

    /// <summary>
    /// Tests for CommandLine option parsing that go beyond the basic cases
    /// already covered by PublisherCliTests. Every test here exercises either
    /// a branch or a lambda body that was previously uncovered.
    /// </summary>
    public sealed class CommandLineOptionsTests : IDisposable
    {
        public CommandLineOptionsTests()
        {
            _deviceId = Environment.GetEnvironmentVariable("IOTEDGE_DEVICEID");
            Environment.SetEnvironmentVariable("IOTEDGE_DEVICEID", "deviceId");
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("IOTEDGE_DEVICEID", _deviceId);
        }

        // --help-env: writes env-var JSON to stdout and exits 0
        [Fact]
        public void HelpEnvOptionExitsWithCodeZero()
        {
            var result = new CommandLineTest(["--help-env"]);

            Assert.Equal(0, result.ExitCode);
        }

        // --help-mm: writes messaging-profile table but does NOT call ExitProcess
        // (unlike --help and --help-env which both call ExitProcess(0))
        [Fact]
        public void HelpMessageProfilesOptionDoesNotExitProcess()
        {
            var result = new CommandLineTest(["--help-mm"]);

            Assert.Equal(-1, result.ExitCode);
            Assert.Empty(result.CommandLine);
        }

        // -f / --pf=<file>  →  PublishedNodesFileKey
        [Fact]
        public void PublishFileShortOptionSetsPublishedNodesFileKey()
        {
            var result = new CommandLineTest(["--pf=custom.json"]);

            Assert.Equal("custom.json", result.CommandLine[PublisherConfig.PublishedNodesFileKey]);
        }

        // --cf (no value)  →  "True"
        [Fact]
        public void CreateIfNotExistWithoutValueDefaultsToTrue()
        {
            var result = new CommandLineTest(["--cf"]);

            Assert.Equal("True", result.CommandLine[PublisherConfig.CreatePublishFileIfNotExistKey]);
        }

        // --cf=false  →  "False"
        [Fact]
        public void CreateIfNotExistWithFalseValueSetsFalse()
        {
            var result = new CommandLineTest(["--cf=false"]);

            Assert.Equal("False", result.CommandLine[PublisherConfig.CreatePublishFileIfNotExistKey]);
        }

        // --id=<value>  →  PublisherIdKey
        [Fact]
        public void PublisherIdOptionSetsKey()
        {
            var result = new CommandLineTest(["--id=myPublisher"]);

            Assert.Equal("myPublisher", result.CommandLine[PublisherConfig.PublisherIdKey]);
        }

        // --s=<value>  →  SiteIdKey
        [Fact]
        public void SiteOptionSetsSiteIdKey()
        {
            var result = new CommandLineTest(["--s=mySite"]);

            Assert.Equal("mySite", result.CommandLine[PublisherConfig.SiteIdKey]);
        }

        // --rs (no value)  →  "True"
        [Fact]
        public void RuntimeStateReportingWithoutValueDefaultsToTrue()
        {
            var result = new CommandLineTest(["--rs"]);

            Assert.Equal("True", result.CommandLine[PublisherConfig.EnableRuntimeStateReportingKey]);
        }

        // --rs=false  →  "False"
        [Fact]
        public void RuntimeStateReportingWithFalseValueSetsFalse()
        {
            var result = new CommandLineTest(["--rs=false"]);

            Assert.Equal("False", result.CommandLine[PublisherConfig.EnableRuntimeStateReportingKey]);
        }

        // --pi (no value)  →  blank placeholder " " so Configuration.FileSystem knows to
        //                      use the publishednodes.json directory with a default filename
        [Fact]
        public void InitFileOptionWithoutValueSetsBlankPlaceholder()
        {
            var result = new CommandLineTest(["--pi"]);

            Assert.Equal(" ", result.CommandLine[Configuration.FileSystem.InitFilePathKey]);
        }

        // --pi=<path>  →  stores the exact path string
        [Fact]
        public void InitFileOptionWithValueSetsPath()
        {
            var result = new CommandLineTest(["--pi=requests.http"]);

            Assert.Equal("requests.http", result.CommandLine[Configuration.FileSystem.InitFilePathKey]);
        }

        // --api-key=<value>  →  ApiKeyOverrideKey
        [Fact]
        public void ApiKeyOptionSetsApiKeyKey()
        {
            var result = new CommandLineTest(["--api-key=secret"]);

            Assert.Equal("secret", result.CommandLine[PublisherConfig.ApiKeyOverrideKey]);
        }

        // --mm=PubSub  →  MessagingModeKey
        [Fact]
        public void MessagingModeOptionSetsKey()
        {
            var result = new CommandLineTest(["--mm=PubSub"]);

            Assert.Equal(nameof(MessagingMode.PubSub), result.CommandLine[PublisherConfig.MessagingModeKey]);
        }

        // --me=Json  →  MessageEncodingKey
        [Fact]
        public void MessageEncodingOptionSetsKey()
        {
            var result = new CommandLineTest(["--me=Json"]);

            Assert.Equal(nameof(MessageEncoding.Json), result.CommandLine[PublisherConfig.MessageEncodingKey]);
        }

        // --bi=5000  →  BatchTriggerIntervalKey as TimeSpan.FromMilliseconds(5000)
        [Fact]
        public void BatchIntervalOptionConvertsMillisecondsToTimeSpan()
        {
            var result = new CommandLineTest(["--bi=5000"]);

            Assert.Equal(
                TimeSpan.FromMilliseconds(5000).ToString(),
                result.CommandLine[PublisherConfig.BatchTriggerIntervalKey]);
        }

        // --si=10  (legacy hidden option: seconds → TimeSpan stored in BatchTriggerIntervalKey)
        [Fact]
        public void LegacyIoTHubSendIntervalConvertsSecondsToTimeSpan()
        {
            var result = new CommandLineTest(["--si=10"]);

            Assert.Equal(
                TimeSpan.FromSeconds(10).ToString(),
                result.CommandLine[PublisherConfig.BatchTriggerIntervalKey]);
        }

        // --bs=100  →  BatchSizeKey
        [Fact]
        public void BatchSizeOptionSetsKey()
        {
            var result = new CommandLineTest(["--bs=100"]);

            Assert.Equal("100", result.CommandLine[PublisherConfig.BatchSizeKey]);
        }

        // --ms=65536  →  IoTHubMaxMessageSizeKey
        [Fact]
        public void MaxMessageSizeOptionSetsKey()
        {
            var result = new CommandLineTest(["--ms=65536"]);

            Assert.Equal("65536", result.CommandLine[PublisherConfig.IoTHubMaxMessageSizeKey]);
        }

        // --t=Mqtt  →  DefaultTransportKey
        [Fact]
        public void DefaultTransportOptionSetsKey()
        {
            var result = new CommandLineTest([$"--t={nameof(WriterGroupTransport.Mqtt)}"]);

            Assert.Equal(nameof(WriterGroupTransport.Mqtt), result.CommandLine[PublisherConfig.DefaultTransportKey]);
        }

        // --b=<cs>  →  MqttClientConnectionStringKey
        [Fact]
        public void MqttConnectionStringOptionSetsKey()
        {
            var result = new CommandLineTest(["--b=HostName=broker;Port=1883"]);

            Assert.Equal(
                "HostName=broker;Port=1883",
                result.CommandLine[Configuration.MqttBroker.MqttClientConnectionStringKey]);
        }

        // --o=<dir>  →  OutputRootKey
        [Fact]
        public void OutputDirOptionSetsKey()
        {
            var result = new CommandLineTest(["--o=./output"]);

            Assert.Equal("./output", result.CommandLine[Configuration.FileSystem.OutputRootKey]);
        }

        // --p=8080  →  HttpServerPortKey
        [Fact]
        public void HttpServerPortOptionSetsKey()
        {
            var result = new CommandLineTest(["--p=8080"]);

            Assert.Equal("8080", result.CommandLine[PublisherConfig.HttpServerPortKey]);
        }

        // --unsecurehttp (no value)  →  UnsecureHttpServerPortKey = default port string
        [Fact]
        public void UnsecureHttpWithoutPortUsesDefaultPort()
        {
            var result = new CommandLineTest(["--unsecurehttp"]);

            Assert.Equal(
                PublisherConfig.UnsecureHttpServerPortDefault.ToString(CultureInfo.CurrentCulture),
                result.CommandLine[PublisherConfig.UnsecureHttpServerPortKey]);
        }

        // --unsecurehttp=9090  →  "9090"
        [Fact]
        public void UnsecureHttpWithPortOverridesDefault()
        {
            var result = new CommandLineTest(["--unsecurehttp=9090"]);

            Assert.Equal("9090", result.CommandLine[PublisherConfig.UnsecureHttpServerPortKey]);
        }

        // --mcp (no value)  →  EnableMcpServerKey = "True"
        [Fact]
        public void McpOptionWithoutValueEnablesMcpServer()
        {
            var result = new CommandLineTest(["--mcp"]);

            Assert.Equal("True", result.CommandLine[PublisherConfig.EnableMcpServerKey]);
        }

        // --mcp=false  →  explicit opt out is honored
        [Fact]
        public void McpOptionAcceptsExplicitBoolean()
        {
            var result = new CommandLineTest(["--mcp=false"]);

            Assert.Equal("False", result.CommandLine[PublisherConfig.EnableMcpServerKey]);
        }

        // The MCP tool server is off unless asked for.
        [Fact]
        public void McpServerIsNotEnabledByDefault()
        {
            var result = new CommandLineTest([]);

            Assert.False(result.CommandLine.ContainsKey(PublisherConfig.EnableMcpServerKey));
        }

        // --apt=Directory  →  SetStoreType valid branch → ApplicationCertificateStoreTypeKey = "Directory"
        [Fact]
        public void AppCertStoreTypeDirectoryOptionSetsKey()
        {
            var result = new CommandLineTest(["--apt=Directory"]);

            Assert.Equal("Directory",
                result.CommandLine[OpcUaClientConfig.ApplicationCertificateStoreTypeKey]);
        }

        // --apt=X509Store  →  SetStoreType valid branch
        [Fact]
        public void AppCertStoreTypeX509StoreOptionSetsKey()
        {
            var result = new CommandLineTest(["--apt=X509Store"]);

            Assert.Equal("X509Store",
                result.CommandLine[OpcUaClientConfig.ApplicationCertificateStoreTypeKey]);
        }

        // --apt=FlatDirectory  →  SetStoreType valid branch
        [Fact]
        public void AppCertStoreTypeFlatDirectoryOptionSetsKey()
        {
            var result = new CommandLineTest(["--apt=FlatDirectory"]);

            Assert.Equal(
                FlatCertificateStore.StoreTypeName,
                result.CommandLine[OpcUaClientConfig.ApplicationCertificateStoreTypeKey]);
        }

        // --apt=BadType  →  SetStoreType throws CommandLineOptionException → exit 160
        [Fact]
        public void InvalidCertStoreTypeExitsWithCode160()
        {
            var result = new CommandLineTest(["--apt=BadStoreType"]);

            Assert.Equal(160, result.ExitCode);
        }

        // --ll=Debug  →  LogLevelKey = "Debug"
        [Fact]
        public void LogLevelOptionSetsKey()
        {
            var result = new CommandLineTest(["--ll=Debug"]);

            Assert.Equal(nameof(LogLevel.Debug),
                result.CommandLine[Configuration.LoggingLevel.LogLevelKey]);
        }

        // --mq=v  AND  --ic=v  →  two legacy warnings (tests the legacyOptions.Count > 0 branch
        //                          with multiple options in the same run)
        [Fact]
        public void MultipleLegacyOptionsGenerateMultipleWarnings()
        {
            var result = new CommandLineTest(["--mq=v", "--ic=v"]);

            Assert.Equal(2, result.Warnings.Count);
        }

        // --mm=NotAMode  →  enum conversion fails → parse exception → exit 160
        [Fact]
        public void InvalidMessagingModeEnumExitsWithCode160()
        {
            var result = new CommandLineTest(["--mm=NotAMode"]);

            Assert.Equal(160, result.ExitCode);
        }

        // --mdt (no value)  →  lambda receives null string → !IsNullOrEmpty(null)=false → MetadataTopicTemplateDefault
        [Fact]
        public void MetadataTopicTemplateWithoutValueUsesDefault()
        {
            var result = new CommandLineTest(["--mdt"]);

            Assert.Equal(
                PublisherConfig.MetadataTopicTemplateDefault,
                result.CommandLine[PublisherConfig.DataSetMetaDataTopicTemplateKey]);
        }

        // --mdt=custom/topic  →  non-empty string is stored as-is
        [Fact]
        public void MetadataTopicTemplateWithValueStoresValue()
        {
            var result = new CommandLineTest(["--mdt=custom/topic"]);

            Assert.Equal("custom/topic",
                result.CommandLine[PublisherConfig.DataSetMetaDataTopicTemplateKey]);
        }

        // --stt (no value)  →  lambda receives null → SchemaTopicTemplateDefault
        [Fact]
        public void SchemaTopicTemplateWithoutValueUsesDefault()
        {
            var result = new CommandLineTest(["--stt"]);

            Assert.Equal(
                PublisherConfig.SchemaTopicTemplateDefault,
                result.CommandLine[PublisherConfig.SchemaTopicTemplateKey]);
        }

        // --stt=custom/schema  →  non-empty string stored as-is
        [Fact]
        public void SchemaTopicTemplateWithValueStoresValue()
        {
            var result = new CommandLineTest(["--stt=custom/schema"]);

            Assert.Equal("custom/schema",
                result.CommandLine[PublisherConfig.SchemaTopicTemplateKey]);
        }

        // --doa  →  DisableOpenApiEndpointKey = "True"
        [Fact]
        public void DisableOpenApiOptionSetsKey()
        {
            var result = new CommandLineTest(["--doa"]);

            Assert.Equal("True", result.CommandLine[PublisherConfig.DisableOpenApiEndpointKey]);
        }

        // --pol=true  →  UseFileChangePollingKey = "True"
        [Fact]
        public void UsePollingWithTrueValueSetsKey()
        {
            var result = new CommandLineTest(["--pol=true"]);

            Assert.Equal("True", result.CommandLine[PublisherConfig.UseFileChangePollingKey]);
        }

        // --pol (no value)  →  UseFileChangePollingKey = "True"
        [Fact]
        public void UsePollingWithoutValueDefaultsToTrue()
        {
            var result = new CommandLineTest(["--pol"]);

            Assert.Equal("True", result.CommandLine[PublisherConfig.UseFileChangePollingKey]);
        }

        // --doa=false  →  DisableOpenApiEndpointKey = "False"
        [Fact]
        public void DisableOpenApiWithFalseValueSetsFalse()
        {
            var result = new CommandLineTest(["--doa=false"]);

            Assert.Equal("False", result.CommandLine[PublisherConfig.DisableOpenApiEndpointKey]);
        }

        // --sl (no value)  →  EnableOpcUaStackLoggingKey = "True"
        [Fact]
        public void StackLoggingWithoutValueDefaultsToTrue()
        {
            var result = new CommandLineTest(["--sl"]);

            Assert.Equal("True", result.CommandLine[OpcUaClientConfig.EnableOpcUaStackLoggingKey]);
        }

        // --sl=false  →  EnableOpcUaStackLoggingKey = "False"
        [Fact]
        public void StackLoggingWithFalseValueSetsFalse()
        {
            var result = new CommandLineTest(["--sl=false"]);

            Assert.Equal("False", result.CommandLine[OpcUaClientConfig.EnableOpcUaStackLoggingKey]);
        }

        // --ksf (no value)  →  OpcUaKeySetLogFolderNameKey = CurrentDirectory
        [Fact]
        public void KeySetLogFolderWithoutValueUsesCurrentDirectory()
        {
            var result = new CommandLineTest(["--ksf"]);

            Assert.Equal(
                System.IO.Directory.GetCurrentDirectory(),
                result.CommandLine[OpcUaClientConfig.OpcUaKeySetLogFolderNameKey]);
        }

        // --ksf=custom/path  →  OpcUaKeySetLogFolderNameKey = "custom/path"
        [Fact]
        public void KeySetLogFolderWithValueSetsPath()
        {
            var result = new CommandLineTest(["--ksf=custom/path"]);

            Assert.Equal("custom/path",
                result.CommandLine[OpcUaClientConfig.OpcUaKeySetLogFolderNameKey]);
        }

        // --ecw (no value)  →  Configuration.ConsoleWriter.EnableKey = "True"
        [Fact]
        public void EnableConsoleWriterWithoutValueDefaultsToTrue()
        {
            var result = new CommandLineTest(["--ecw"]);

            Assert.Equal("True", result.CommandLine[Configuration.ConsoleWriter.EnableKey]);
        }

        // --di=30  →  DiagnosticsIntervalKey = TimeSpan.FromSeconds(30)
        [Fact]
        public void DiagnosticsIntervalOptionConvertsSecondsToTimeSpan()
        {
            var result = new CommandLineTest(["--di=30"]);

            Assert.Equal(
                TimeSpan.FromSeconds(30).ToString(),
                result.CommandLine[PublisherConfig.DiagnosticsIntervalKey]);
        }

        // --pd=Events  →  DiagnosticsTargetKey = "Events"
        [Fact]
        public void DiagnosticsTargetOptionSetsKey()
        {
            var result = new CommandLineTest(["--pd=Events"]);

            Assert.Equal(
                nameof(PublisherDiagnosticTargetType.Events),
                result.CommandLine[PublisherConfig.DiagnosticsTargetKey]);
        }

        // --dr (no value)  →  DisableResourceMonitoringKey = "True"
        [Fact]
        public void DisableResourceMonitoringWithoutValueDefaultsToTrue()
        {
            var result = new CommandLineTest(["--dr"]);

            Assert.Equal("True", result.CommandLine[PublisherConfig.DisableResourceMonitoringKey]);
        }

        // --ln (no value)  →  DebugLogNotificationsKey = "True"
        [Fact]
        public void LogNotificationsWithoutValueDefaultsToTrue()
        {
            var result = new CommandLineTest(["--ln"]);

            Assert.Equal("True", result.CommandLine[PublisherConfig.DebugLogNotificationsKey]);
        }

        // --lnh (no value)  →  DebugLogNotificationsWithHeartbeatKey = "True"
        [Fact]
        public void LogNotificationsWithHeartbeatWithoutValueDefaultsToTrue()
        {
            var result = new CommandLineTest(["--lnh"]);

            Assert.Equal("True",
                result.CommandLine[PublisherConfig.DebugLogNotificationsWithHeartbeatKey]);
        }

        // --lnf=MyFilter  →  DebugLogNotificationsFilterKey = "MyFilter"
        [Fact]
        public void LogNotificationsFilterWithValueSetsKey()
        {
            var result = new CommandLineTest(["--lnf=MyFilter"]);

            Assert.Equal("MyFilter",
                result.CommandLine[PublisherConfig.DebugLogNotificationsFilterKey]);
        }

        // --len (no value)  →  DebugLogEncodedNotificationsKey = "True"
        [Fact]
        public void LogEncodedNotificationsWithoutValueDefaultsToTrue()
        {
            var result = new CommandLineTest(["--len"]);

            Assert.Equal("True",
                result.CommandLine[PublisherConfig.DebugLogEncodedNotificationsKey]);
        }

        // --oc=http://collector:4317  →  OtlpCollectorEndpointKey
        [Fact]
        public void OtlpCollectorEndpointOptionSetsKey()
        {
            var result = new CommandLineTest(["--oc=http://collector:4317"]);

            Assert.Equal("http://collector:4317",
                result.CommandLine[Configuration.Otel.OtlpCollectorEndpointKey]);
        }

        // --eol (no value)  →  EnableOtelLoggingKey = "True"
        [Fact]
        public void EnableOtelLoggingWithoutValueDefaultsToTrue()
        {
            var result = new CommandLineTest(["--eol"]);

            Assert.Equal("True", result.CommandLine[Configuration.Otel.EnableOtelLoggingKey]);
        }

        // --eot (no value)  →  EnableOtelTracesKey = "True"
        [Fact]
        public void EnableOtelTracesWithoutValueDefaultsToTrue()
        {
            var result = new CommandLineTest(["--eot"]);

            Assert.Equal("True", result.CommandLine[Configuration.Otel.EnableOtelTracesKey]);
        }

        // --oxi=5000  →  OtlpExportIntervalMillisecondsKey = TimeSpan.FromMilliseconds(5000)
        [Fact]
        public void OtlpExportIntervalOptionConvertsMillisecondsToTimeSpan()
        {
            var result = new CommandLineTest(["--oxi=5000"]);

            Assert.Equal(
                TimeSpan.FromMilliseconds(5000).ToString(),
                result.CommandLine[Configuration.Otel.OtlpExportIntervalMillisecondsKey]);
        }

        // --mms=2000  →  OtlpMaxMetricStreamsKey = "2000"
        [Fact]
        public void MaxMetricStreamsOptionSetsKey()
        {
            var result = new CommandLineTest(["--mms=2000"]);

            Assert.Equal("2000",
                result.CommandLine[Configuration.Otel.OtlpMaxMetricStreamsKey]);
        }

        // --em (no value)  →  EnableMetricsKey = "True"
        [Fact]
        public void EnablePrometheusEndpointWithoutValueDefaultsToTrue()
        {
            var result = new CommandLineTest(["--em"]);

            Assert.Equal("True", result.CommandLine[Configuration.Otel.EnableMetricsKey]);
        }

        // --ari (no value)  →  OtlpRuntimeInstrumentationKey = "True"
        [Fact]
        public void AddRuntimeInstrumentationWithoutValueDefaultsToTrue()
        {
            var result = new CommandLineTest(["--ari"]);

            Assert.Equal("True",
                result.CommandLine[Configuration.Otel.OtlpRuntimeInstrumentationKey]);
        }

        // --ats (no value)  →  OtlpTotalNameSuffixForCountersKey = "True"
        [Fact]
        public void AddTotalNameSuffixWithoutValueDefaultsToTrue()
        {
            var result = new CommandLineTest(["--ats"]);

            Assert.Equal("True",
                result.CommandLine[Configuration.Otel.OtlpTotalNameSuffixForCountersKey]);
        }

        // --sc=5  →  ScaleTestCountKey = "5"
        [Fact]
        public void ScaleTestCountOptionSetsKey()
        {
            var result = new CommandLineTest(["--sc=5"]);

            Assert.Equal("5", result.CommandLine[PublisherConfig.ScaleTestCountKey]);
        }

        // --tp=<path>  →  TrustedPeerCertificatesPathKey
        [Fact]
        public void TrustedCertStorePathOptionSetsKey()
        {
            var result = new CommandLineTest(["--tp=/certs/trusted"]);

            Assert.Equal("/certs/trusted",
                result.CommandLine[OpcUaClientConfig.TrustedPeerCertificatesPathKey]);
        }

        // --tpt=Directory  →  TrustedPeerCertificatesTypeKey
        [Fact]
        public void TrustedCertStoreTypeDirectoryOptionSetsKey()
        {
            var result = new CommandLineTest(["--tpt=Directory"]);

            Assert.Equal("Directory",
                result.CommandLine[OpcUaClientConfig.TrustedPeerCertificatesTypeKey]);
        }

        // --rp=<path>  →  RejectedCertificateStorePathKey
        [Fact]
        public void RejectedCertStorePathOptionSetsKey()
        {
            var result = new CommandLineTest(["--rp=/certs/rejected"]);

            Assert.Equal("/certs/rejected",
                result.CommandLine[OpcUaClientConfig.RejectedCertificateStorePathKey]);
        }

        // --ip=<path>  →  TrustedIssuerCertificatesPathKey
        [Fact]
        public void IssuerCertStorePathOptionSetsKey()
        {
            var result = new CommandLineTest(["--ip=/certs/issuers"]);

            Assert.Equal("/certs/issuers",
                result.CommandLine[OpcUaClientConfig.TrustedIssuerCertificatesPathKey]);
        }

        // --up=<path>  →  TrustedUserCertificatesPathKey
        [Fact]
        public void UserCertStorePathOptionSetsKey()
        {
            var result = new CommandLineTest(["--up=/certs/user"]);

            Assert.Equal("/certs/user",
                result.CommandLine[OpcUaClientConfig.TrustedUserCertificatesPathKey]);
        }

        // --uip=<path>  →  UserIssuerCertificatesPathKey
        [Fact]
        public void UserIssuerCertStorePathOptionSetsKey()
        {
            var result = new CommandLineTest(["--uip=/certs/user/issuer"]);

            Assert.Equal("/certs/user/issuer",
                result.CommandLine[OpcUaClientConfig.UserIssuerCertificatesPathKey]);
        }

        // --cp is a legacy option — it is silently accepted but does NOT
        // inject ApplicationCertificatePasswordKey into configuration.
        [Fact]
        public void LegacyCertPasswordOptionIsAcceptedWithoutThrowing()
        {
            // Must not throw; the option is recognised and discarded.
            var result = new CommandLineTest(["--cp=s3cr3t"]);

            Assert.False(result.CommandLine.ContainsKey(
                OpcUaClientConfig.ApplicationCertificatePasswordKey));
        }

        private readonly string? _deviceId;
    }
}
