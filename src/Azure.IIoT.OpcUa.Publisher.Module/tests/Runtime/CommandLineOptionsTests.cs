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

        private readonly string? _deviceId;
    }
}
