// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services
{
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using FluentAssertions;
    using Azure.IIoT.OpcUa.Core;
    using Azure.IIoT.OpcUa.Core.IoTEdge.Services;
    using Azure.IIoT.OpcUa.Core.Logging;
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Azure.IIoT.OpcUa.Core.Storage.Services;
    using Microsoft.Extensions.Configuration;
    using Moq;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class RuntimeStateReporterTests
    {
        [Fact]
        public async Task ReportingDisabledTestAsync()
        {
            var client = new Mock<IEventClient>();
            var collector = new Mock<IDiagnosticCollector>();

            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            // This will disable state reporting.
            options.Value.EnableRuntimeStateReporting = false;

            var _logger = Log.Console<RuntimeStateReporter>();

            using var runtimeStateReporter = new RuntimeStateReporter(
                client.Object.YieldReturn(),
                new MemoryKVStore().YieldReturn(),
                options,
                collector.Object,
                _logger);

            await FluentActions
                .Invoking(async () => await runtimeStateReporter.SendRestartAnnouncementAsync(default))
                .Should()
                .NotThrowAsync()
                ;
            client.Verify(c => c.CreateEvent(), Times.Never());
        }

        [Fact]
        public async Task ClientNotInitializedTestAsync()
        {
            var client = new Mock<IEventClient>();
            var collector = new Mock<IDiagnosticCollector>();
            client.Setup(m => m.CreateEvent()).Throws<IOException>();

            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.EnableRuntimeStateReporting = true;

            var _logger = Log.Console<RuntimeStateReporter>();

            using var runtimeStateReporter = new RuntimeStateReporter(
                client.Object.YieldReturn(),
                new MemoryKVStore().YieldReturn(),
                options,
                collector.Object,
                _logger);

            await FluentActions
                .Invoking(async () => await runtimeStateReporter.SendRestartAnnouncementAsync(default))
                .Should()
                .NotThrowAsync()
                ;
        }

        [Fact]
        public async Task CertificateIsPersistedAsBase64AndReusedTestAsync()
        {
            var client = new Mock<IEventClient>();
            var collector = new Mock<IDiagnosticCollector>();
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            var store = new MemoryKVStore();
            var logger = Log.Console<RuntimeStateReporter>();

            string thumbprint;
            using (var reporter = new RuntimeStateReporter(
                client.Object.YieldReturn(), store.YieldReturn(), options,
                collector.Object, logger))
            {
                await reporter.SendRestartAnnouncementAsync(default);

                Assert.NotNull(reporter.Certificate);
                thumbprint = reporter.Certificate.Thumbprint;
                var encoded = (string?)store.State[
                    OpcUa.Constants.TwinPropertyCertificateKey];
                Assert.False(string.IsNullOrEmpty(encoded));
                Assert.NotEmpty(Convert.FromBase64String(encoded));
            }

            using var restarted = new RuntimeStateReporter(
                client.Object.YieldReturn(), store.YieldReturn(), options,
                collector.Object, logger);
            await restarted.SendRestartAnnouncementAsync(default);

            Assert.NotNull(restarted.Certificate);
            Assert.Equal(thumbprint, restarted.Certificate.Thumbprint);
        }

        [Fact]
        public async Task ReportingTestAsync()
        {
            var _client = new Mock<IEventClient>();
            var collector = new Mock<IDiagnosticCollector>();

            var _message = new Mock<IEvent>()
                .SetupAllProperties();
            _message
                .Setup(m => m.Dispose());
            _client
                .Setup(c => c.CreateEvent())
                .Returns(_message.Object);
            _message
                .Setup(c => c.SendAsync(It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            var contentType = string.Empty;
            var contentEncoding = string.Empty;
            var routingInfo = string.Empty;
            List<ReadOnlySequence<byte>> buffers = null;
            _message.Setup(c => c.SetRetain(It.Is<bool>(v => v)))
                .Returns(_message.Object);
            _message.Setup(c => c.AddProperty(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((k, v) =>
                {
                    if (k == OpcUa.Constants.MessagePropertyRoutingKey)
                    {
                        routingInfo = v;
                    }
                })
                .Returns(_message.Object);
            _message.Setup(c => c.SetContentType(It.IsAny<string>()))
                .Callback<string>(v => contentType = v)
                .Returns(_message.Object);
            _message.Setup(c => c.SetContentEncoding(It.IsAny<string>()))
                .Callback<string>(v => contentEncoding = v)
                .Returns(_message.Object);
            _message.Setup(c => c.AddBuffers(It.IsAny<IEnumerable<ReadOnlySequence<byte>>>()))
                .Callback<IEnumerable<ReadOnlySequence<byte>>>(v => buffers = v.ToList())
                .Returns(_message.Object);
            _message.Setup(c => c.SetTopic(It.IsAny<string>()))
                .Returns(_message.Object);

            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.EnableRuntimeStateReporting = true;
            options.Value.RuntimeStateRoutingInfo = "runtimeinfo";

            var _logger = Log.Console<RuntimeStateReporter>();

            using var runtimeStateReporter = new RuntimeStateReporter(
                _client.Object.YieldReturn(),
                new MemoryKVStore().YieldReturn(),
                options,
                collector.Object,
                _logger);

            await FluentActions
                .Invoking(async () => await runtimeStateReporter.SendRestartAnnouncementAsync(default))
                .Should()
                .NotThrowAsync()
                ;

            _message.Verify(c => c.SendAsync(It.IsAny<CancellationToken>()), Times.Once());
            _message.Verify(m => m.Dispose(), Times.Once());

            Assert.Equal("runtimeinfo", routingInfo);
            Assert.Equal(ContentMimeType.Json, contentType);
            Assert.Equal(Encoding.UTF8.WebName, contentEncoding);

            Assert.Single(buffers);
            var body = Encoding.UTF8.GetString(buffers[0].FirstSpan);
            Assert.StartsWith("{\"MessageType\":\"RestartAnnouncement\",\"MessageVersion\":1,\"TimestampUtc\":", body, StringComparison.Ordinal);
        }

        [Fact]
        public async Task ApiKeyOverrideIsUsedAsApiKeyAsync()
        {
            var client = new Mock<IEventClient>();
            var collector = new Mock<IDiagnosticCollector>();
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.ApiKeyOverride = "my-override-key-12345";
            var logger = Log.Console<RuntimeStateReporter>();

            using var reporter = new RuntimeStateReporter(
                client.Object.YieldReturn(),
                new MemoryKVStore().YieldReturn(),
                options,
                collector.Object,
                logger);

            await reporter.SendRestartAnnouncementAsync(default);

            Assert.Equal("my-override-key-12345", reporter.ApiKey);
        }

        [Fact]
        public async Task AllowedTransportsFiltersOutDisallowedClientsAsync()
        {
            var allowedClient = new Mock<IEventClient>();
            allowedClient.SetupGet(c => c.Name).Returns("IoTHub");
            // Throw so the reporter handles it gracefully; we just want to verify it was called
            allowedClient.Setup(c => c.CreateEvent()).Throws<IOException>();
            var disallowedClient = new Mock<IEventClient>();
            disallowedClient.SetupGet(c => c.Name).Returns("Mqtt");

            var collector = new Mock<IDiagnosticCollector>();
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.EnableRuntimeStateReporting = true;
            // Only IoTHub transport is allowed
            options.Value.AllowedEventAndDiagnosticsTransports.Add(WriterGroupTransport.IoTHub);

            var logger = Log.Console<RuntimeStateReporter>();

            using var reporter = new RuntimeStateReporter(
                new[] { allowedClient.Object, disallowedClient.Object },
                new MemoryKVStore().YieldReturn(),
                options,
                collector.Object,
                logger);

            // Does not throw even though the event client throws
            await reporter.SendRestartAnnouncementAsync(default);

            // The allowed client was attempted; the disallowed one was never tried
            allowedClient.Verify(c => c.CreateEvent(), Times.AtLeastOnce());
            disallowedClient.Verify(c => c.CreateEvent(), Times.Never());
        }

        [Fact]
        public async Task RenewTlsCertificateOnStartupForcesNewCertificateAsync()
        {
            var client = new Mock<IEventClient>();
            var collector = new Mock<IDiagnosticCollector>();
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.RenewTlsCertificateOnStartup = true;
            var store = new MemoryKVStore();
            var logger = Log.Console<RuntimeStateReporter>();

            string firstThumbprint;
            using (var first = new RuntimeStateReporter(
                client.Object.YieldReturn(), store.YieldReturn(), options,
                collector.Object, logger))
            {
                await first.SendRestartAnnouncementAsync(default);
                Assert.NotNull(first.Certificate);
                firstThumbprint = first.Certificate.Thumbprint;
            }

            // With RenewTlsCertificateOnStartup=true a new certificate is always created
            using var second = new RuntimeStateReporter(
                client.Object.YieldReturn(), store.YieldReturn(), options,
                collector.Object, logger);
            await second.SendRestartAnnouncementAsync(default);

            Assert.NotNull(second.Certificate);
            // New certificate thumbprint differs from the first
            Assert.NotEqual(firstThumbprint, second.Certificate.Thumbprint);
        }

        [Fact]
        public void DisposeTwiceDoesNotThrow()
        {
            var client = new Mock<IEventClient>();
            var collector = new Mock<IDiagnosticCollector>();
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            var logger = Log.Console<RuntimeStateReporter>();

            var reporter = new RuntimeStateReporter(
                client.Object.YieldReturn(),
                new MemoryKVStore().YieldReturn(),
                options,
                collector.Object,
                logger);

            reporter.Dispose();
            reporter.Dispose(); // Should not throw
        }

        [Fact]
        public async Task MultipleStoresPicksFirstForApiKeyGenerationAsync()
        {
            var client = new Mock<IEventClient>();
            var collector = new Mock<IDiagnosticCollector>();
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            var store1 = new MemoryKVStore();
            var store2 = new MemoryKVStore();
            var logger = Log.Console<RuntimeStateReporter>();

            using var reporter = new RuntimeStateReporter(
                client.Object.YieldReturn(),
                new[] { store1, store2 },
                options,
                collector.Object,
                logger);

            await reporter.SendRestartAnnouncementAsync(default);

            Assert.NotNull(reporter.ApiKey);
            // API key written to one of the stores
            Assert.True(store1.State.ContainsKey(OpcUa.Constants.TwinPropertyApiKeyKey)
                || store2.State.ContainsKey(OpcUa.Constants.TwinPropertyApiKeyKey));
        }

        [Fact]
        public async Task HttpServerPortSet_StoresSchemeHostnameAndPortAsync()
        {
            var client = new Mock<IEventClient>();
            var collector = new Mock<IDiagnosticCollector>();
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.HttpServerPort = 8443;
            var store = new MemoryKVStore();
            var logger = Log.Console<RuntimeStateReporter>();

            using var reporter = new RuntimeStateReporter(
                client.Object.YieldReturn(),
                store.YieldReturn(),
                options,
                collector.Object,
                logger);

            await reporter.SendRestartAnnouncementAsync(default);

            Assert.Equal("https", (string?)store.State[OpcUa.Constants.TwinPropertySchemeKey]);
            Assert.NotNull(store.State[OpcUa.Constants.TwinPropertyHostnameKey]);
            Assert.Equal(8443, (int?)store.State[OpcUa.Constants.TwinPropertyPortKey]);
        }

        [Fact]
        public async Task HttpServerPortNotSet_StoresNullSchemeHostnameAndPortAsync()
        {
            var client = new Mock<IEventClient>();
            var collector = new Mock<IDiagnosticCollector>();
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.HttpServerPort = null;
            var store = new MemoryKVStore();
            var logger = Log.Console<RuntimeStateReporter>();

            using var reporter = new RuntimeStateReporter(
                client.Object.YieldReturn(),
                store.YieldReturn(),
                options,
                collector.Object,
                logger);

            await reporter.SendRestartAnnouncementAsync(default);

            // With no port configured the store entries should be null
            Assert.True(!store.State.ContainsKey(OpcUa.Constants.TwinPropertySchemeKey)
                || store.State[OpcUa.Constants.TwinPropertySchemeKey] == null);
            Assert.True(!store.State.ContainsKey(OpcUa.Constants.TwinPropertyPortKey)
                || store.State[OpcUa.Constants.TwinPropertyPortKey] == null);
        }

        [Fact]
        public async Task SiteId_IsWrittenToStateStoreAsync()
        {
            var client = new Mock<IEventClient>();
            var collector = new Mock<IDiagnosticCollector>();
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.SiteId = "test-site-42";
            var store = new MemoryKVStore();
            var logger = Log.Console<RuntimeStateReporter>();

            using var reporter = new RuntimeStateReporter(
                client.Object.YieldReturn(),
                store.YieldReturn(),
                options,
                collector.Object,
                logger);

            await reporter.SendRestartAnnouncementAsync(default);

            Assert.Equal("test-site-42", (string?)store.State[OpcUa.Constants.TwinPropertySiteKey]);
        }

        [Fact]
        public async Task EnableCloudEventsTrue_CallsAsCloudEventOnMessageAsync()
        {
            var client = new Mock<IEventClient>();
            var collector = new Mock<IDiagnosticCollector>();

            var message = new Mock<IEvent>();
            message.Setup(m => m.SetTopic(It.IsAny<string?>())).Returns(message.Object);
            message.Setup(m => m.AddBuffers(It.IsAny<IEnumerable<ReadOnlySequence<byte>>>()))
                .Returns(message.Object);
            message.Setup(m => m.SetContentType(It.IsAny<string?>())).Returns(message.Object);
            message.Setup(m => m.SetContentEncoding(It.IsAny<string?>())).Returns(message.Object);
            message.Setup(m => m.SetRetain(It.IsAny<bool>())).Returns(message.Object);
            message.Setup(m => m.AsCloudEvent(It.IsAny<CloudEventHeader>()))
                .Returns(message.Object);
            message.Setup(m => m.SendAsync(It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);
            client.Setup(c => c.CreateEvent()).Returns(message.Object);

            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.EnableRuntimeStateReporting = true;
            options.Value.EnableCloudEvents = true;
            var logger = Log.Console<RuntimeStateReporter>();

            using var reporter = new RuntimeStateReporter(
                client.Object.YieldReturn(),
                new MemoryKVStore().YieldReturn(),
                options,
                collector.Object,
                logger);

            await reporter.SendRestartAnnouncementAsync(default);

            message.Verify(m => m.AsCloudEvent(It.IsAny<CloudEventHeader>()), Times.Once());
        }

        [Fact]
        public async Task EnableCloudEventsFalse_DoesNotCallAsCloudEventAsync()
        {
            var client = new Mock<IEventClient>();
            var collector = new Mock<IDiagnosticCollector>();

            var message = new Mock<IEvent>();
            message.Setup(m => m.SetTopic(It.IsAny<string?>())).Returns(message.Object);
            message.Setup(m => m.AddBuffers(It.IsAny<IEnumerable<ReadOnlySequence<byte>>>()))
                .Returns(message.Object);
            message.Setup(m => m.SetContentType(It.IsAny<string?>())).Returns(message.Object);
            message.Setup(m => m.SetContentEncoding(It.IsAny<string?>())).Returns(message.Object);
            message.Setup(m => m.SetRetain(It.IsAny<bool>())).Returns(message.Object);
            message.Setup(m => m.AddProperty(It.IsAny<string>(), It.IsAny<string?>()))
                .Returns(message.Object);
            message.Setup(m => m.SendAsync(It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);
            client.Setup(c => c.CreateEvent()).Returns(message.Object);

            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.EnableRuntimeStateReporting = true;
            options.Value.EnableCloudEvents = false;
            var logger = Log.Console<RuntimeStateReporter>();

            using var reporter = new RuntimeStateReporter(
                client.Object.YieldReturn(),
                new MemoryKVStore().YieldReturn(),
                options,
                collector.Object,
                logger);

            await reporter.SendRestartAnnouncementAsync(default);

            message.Verify(m => m.AsCloudEvent(It.IsAny<CloudEventHeader>()), Times.Never());
        }

        [Fact]
        public void WriteDiagnosticsToConsole_EmptyDiagnostics_ProducesNoOutput()
        {
            var method = typeof(RuntimeStateReporter).GetMethod(
                "WriteDiagnosticsToConsole",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(method);

            var oldOut = Console.Out;
            using var capture = new StringWriter();
            Console.SetOut(capture);
            try
            {
                method.Invoke(null, [
                    Array.Empty<(string, WriterGroupDiagnosticModel)>(),
                    true
                ]);
            }
            finally
            {
                Console.SetOut(oldOut);
            }

            Assert.Empty(capture.ToString().Trim());
        }

        [Fact]
        public void WriteDiagnosticsToConsole_WithSingleGroupAndNoResourceInfo_ContainsGroupName()
        {
            var method = typeof(RuntimeStateReporter).GetMethod(
                "WriteDiagnosticsToConsole",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(method);

            var diagnostics = new List<(string, WriterGroupDiagnosticModel)>
            {
                ("wg-1", new WriterGroupDiagnosticModel
                {
                    WriterGroupName = "MyGroup",
                    PublisherVersion = "3.0.0",
                    IngestionDuration = TimeSpan.FromMinutes(2),
                    Timestamp = DateTimeOffset.UtcNow
                })
            };

            var oldOut = Console.Out;
            using var capture = new StringWriter();
            Console.SetOut(capture);
            try
            {
                method.Invoke(null, [(IEnumerable<(string, WriterGroupDiagnosticModel)>)diagnostics, false]);
            }
            finally
            {
                Console.SetOut(oldOut);
            }

            var output = capture.ToString();
            Assert.Contains("DIAGNOSTICS INFORMATION", output, StringComparison.Ordinal);
            Assert.Contains("MyGroup", output, StringComparison.Ordinal);
            Assert.DoesNotContain("Cpu", output, StringComparison.Ordinal);
        }

        [Fact]
        public void WriteDiagnosticsToConsole_WithResourceInfo_IncludesCpuAndMemoryLines()
        {
            var method = typeof(RuntimeStateReporter).GetMethod(
                "WriteDiagnosticsToConsole",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(method);

            var diagnostics = new List<(string, WriterGroupDiagnosticModel)>
            {
                ("wg-1", new WriterGroupDiagnosticModel
                {
                    WriterGroupName = "ResourceGroup",
                    PublisherVersion = "3.0.0",
                    IngestionDuration = TimeSpan.FromSeconds(60),
                    CpuLimitUtilization = 0.75,
                    CpuUsedPercentage = 0.40,
                    MemoryLimitUtilization = 0.50,
                    MemoryUsedPercentage = 0.30,
                    MemoryUsedInBytes = 256_000
                })
            };

            var oldOut = Console.Out;
            using var capture = new StringWriter();
            Console.SetOut(capture);
            try
            {
                method.Invoke(null, [(IEnumerable<(string, WriterGroupDiagnosticModel)>)diagnostics, true]);
            }
            finally
            {
                Console.SetOut(oldOut);
            }

            var output = capture.ToString();
            Assert.Contains("Cpu", output, StringComparison.Ordinal);
            Assert.Contains("Memory", output, StringComparison.Ordinal);
        }

        [Fact]
        public void WriteDiagnosticsToConsole_WithNonZeroCounters_IncludesRateSuffix()
        {
            var method = typeof(RuntimeStateReporter).GetMethod(
                "WriteDiagnosticsToConsole",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(method);

            var diagnostics = new List<(string, WriterGroupDiagnosticModel)>
            {
                ("wg-2", new WriterGroupDiagnosticModel
                {
                    WriterGroupName = "CounterGroup",
                    PublisherVersion = "3.0.0",
                    IngestionDuration = TimeSpan.FromSeconds(120),
                    OutgressIoTMessageCount = 50,
                    SentMessagesPerSec = 2.5,
                    IngressKeepAliveNotifications = 10,
                    IngressValueChanges = 100,
                    IngressValueChangesInLastMinute = 60,
                    IngressDataChanges = 80,
                    IngressDataChangesInLastMinute = 40,
                    IngressEvents = 20,
                    IngressEventsInLastMinute = 15,
                    IngressEventNotifications = 25,
                    IngressEventNotificationsInLastMinute = 18,
                    NumberOfConnectedEndpoints = 2,
                    NumberOfDisconnectedEndpoints = 1,
                    ConnectionsReconnecting = 1,
                    EncoderAvgIoTMessageBodySize = 512,
                    EncoderAvgIoTChunkUsage = 0.8
                })
            };

            var oldOut = Console.Out;
            using var capture = new StringWriter();
            Console.SetOut(capture);
            try
            {
                method.Invoke(null, [(IEnumerable<(string, WriterGroupDiagnosticModel)>)diagnostics, false]);
            }
            finally
            {
                Console.SetOut(oldOut);
            }

            var output = capture.ToString();
            Assert.Contains("All time", output, StringComparison.Ordinal);
            Assert.Contains("CounterGroup", output, StringComparison.Ordinal);
            Assert.Contains("Partially Connected", output, StringComparison.Ordinal);
            Assert.Contains("reconnecting", output, StringComparison.Ordinal);
        }

        [Fact]
        public void WriteDiagnosticsToConsole_ConnectedOnly_ShowsConnectedState()
        {
            var method = typeof(RuntimeStateReporter).GetMethod(
                "WriteDiagnosticsToConsole",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(method);

            var diagnostics = new List<(string, WriterGroupDiagnosticModel)>
            {
                ("wg-3", new WriterGroupDiagnosticModel
                {
                    WriterGroupName = "ConnectedGroup",
                    NumberOfConnectedEndpoints = 3,
                    NumberOfDisconnectedEndpoints = 0
                })
            };

            var oldOut = Console.Out;
            using var capture = new StringWriter();
            Console.SetOut(capture);
            try
            {
                method.Invoke(null, [(IEnumerable<(string, WriterGroupDiagnosticModel)>)diagnostics, false]);
            }
            finally
            {
                Console.SetOut(oldOut);
            }

            Assert.Contains("(Connected)", capture.ToString(), StringComparison.Ordinal);
        }

        [Fact]
        public void WriteDiagnosticsToConsole_DisconnectedOnly_ShowsDisconnectedState()
        {
            var method = typeof(RuntimeStateReporter).GetMethod(
                "WriteDiagnosticsToConsole",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(method);

            var diagnostics = new List<(string, WriterGroupDiagnosticModel)>
            {
                ("wg-4", new WriterGroupDiagnosticModel
                {
                    WriterGroupName = "DisconnectedGroup",
                    NumberOfConnectedEndpoints = 0,
                    NumberOfDisconnectedEndpoints = 2
                })
            };

            var oldOut = Console.Out;
            using var capture = new StringWriter();
            Console.SetOut(capture);
            try
            {
                method.Invoke(null, [(IEnumerable<(string, WriterGroupDiagnosticModel)>)diagnostics, false]);
            }
            finally
            {
                Console.SetOut(oldOut);
            }

            Assert.Contains("(Disconnected)", capture.ToString(), StringComparison.Ordinal);
        }

        [Fact]
        public async Task SendDiagnosticsAsync_WithDiagnosticGroup_SendsEventAsync()
        {
            var client = new Mock<IEventClient>();
            var collector = new Mock<IDiagnosticCollector>();

            var message = new Mock<IEvent>();
            message.Setup(m => m.SetTopic(It.IsAny<string?>())).Returns(message.Object);
            message.Setup(m => m.AddBuffers(It.IsAny<IEnumerable<ReadOnlySequence<byte>>>())).Returns(message.Object);
            message.Setup(m => m.SetContentType(It.IsAny<string?>())).Returns(message.Object);
            message.Setup(m => m.SetContentEncoding(It.IsAny<string?>())).Returns(message.Object);
            message.Setup(m => m.SetRetain(It.IsAny<bool>())).Returns(message.Object);
            message.Setup(m => m.SetTtl(It.IsAny<TimeSpan>())).Returns(message.Object);
            message.Setup(m => m.AddProperty(It.IsAny<string>(), It.IsAny<string?>())).Returns(message.Object);
            message.Setup(m => m.SendAsync(It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
            client.Setup(c => c.CreateEvent()).Returns(message.Object);

            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.DiagnosticsTarget = PublisherDiagnosticTargetType.Events;
            var logger = Log.Console<RuntimeStateReporter>();

            using var reporter = new RuntimeStateReporter(
                client.Object.YieldReturn(),
                new MemoryKVStore().YieldReturn(),
                options,
                collector.Object,
                logger);

            var diagnostics = new List<(string, WriterGroupDiagnosticModel)>
            {
                ("group-id-1", new WriterGroupDiagnosticModel
                {
                    WriterGroupName = "TestDiagGroup",
                    IngestionDuration = TimeSpan.FromSeconds(10)
                })
            };

            var method = typeof(RuntimeStateReporter).GetMethod(
                "SendDiagnosticsAsync",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            await (ValueTask)method.Invoke(reporter, [
                (IEnumerable<(string, WriterGroupDiagnosticModel)>)diagnostics,
                CancellationToken.None
            ])!;

            message.Verify(m => m.SendAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task SendDiagnosticsAsync_WithCloudEvents_CallsAsCloudEventAsync()
        {
            var client = new Mock<IEventClient>();
            var collector = new Mock<IDiagnosticCollector>();

            var message = new Mock<IEvent>();
            message.Setup(m => m.SetTopic(It.IsAny<string?>())).Returns(message.Object);
            message.Setup(m => m.AddBuffers(It.IsAny<IEnumerable<ReadOnlySequence<byte>>>())).Returns(message.Object);
            message.Setup(m => m.SetContentType(It.IsAny<string?>())).Returns(message.Object);
            message.Setup(m => m.SetContentEncoding(It.IsAny<string?>())).Returns(message.Object);
            message.Setup(m => m.SetRetain(It.IsAny<bool>())).Returns(message.Object);
            message.Setup(m => m.SetTtl(It.IsAny<TimeSpan>())).Returns(message.Object);
            message.Setup(m => m.AsCloudEvent(It.IsAny<CloudEventHeader>())).Returns(message.Object);
            message.Setup(m => m.SendAsync(It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
            client.Setup(c => c.CreateEvent()).Returns(message.Object);

            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.EnableCloudEvents = true;
            options.Value.DiagnosticsTarget = PublisherDiagnosticTargetType.Events;
            options.Value.PublisherId = "my-publisher";
            var logger = Log.Console<RuntimeStateReporter>();

            using var reporter = new RuntimeStateReporter(
                client.Object.YieldReturn(),
                new MemoryKVStore().YieldReturn(),
                options,
                collector.Object,
                logger);

            var diagnostics = new List<(string, WriterGroupDiagnosticModel)>
            {
                ("grp-1", new WriterGroupDiagnosticModel { WriterGroupName = "CloudGroup" })
            };

            var method = typeof(RuntimeStateReporter).GetMethod(
                "SendDiagnosticsAsync",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            await (ValueTask)method.Invoke(reporter, [
                (IEnumerable<(string, WriterGroupDiagnosticModel)>)diagnostics,
                CancellationToken.None
            ])!;

            message.Verify(m => m.AsCloudEvent(It.IsAny<CloudEventHeader>()), Times.Once());
        }

        [Fact]
        public async Task SendDiagnosticsAsync_WithRoutingInfo_AddsRoutingPropertyAsync()
        {
            var client = new Mock<IEventClient>();
            var collector = new Mock<IDiagnosticCollector>();

            var capturedProperties = new Dictionary<string, string?>();
            var message = new Mock<IEvent>();
            message.Setup(m => m.SetTopic(It.IsAny<string?>())).Returns(message.Object);
            message.Setup(m => m.AddBuffers(It.IsAny<IEnumerable<ReadOnlySequence<byte>>>())).Returns(message.Object);
            message.Setup(m => m.SetContentType(It.IsAny<string?>())).Returns(message.Object);
            message.Setup(m => m.SetContentEncoding(It.IsAny<string?>())).Returns(message.Object);
            message.Setup(m => m.SetRetain(It.IsAny<bool>())).Returns(message.Object);
            message.Setup(m => m.SetTtl(It.IsAny<TimeSpan>())).Returns(message.Object);
            message.Setup(m => m.AddProperty(It.IsAny<string>(), It.IsAny<string?>()))
                .Callback<string, string?>((k, v) => capturedProperties[k] = v)
                .Returns(message.Object);
            message.Setup(m => m.SendAsync(It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
            client.Setup(c => c.CreateEvent()).Returns(message.Object);

            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.EnableCloudEvents = false;
            options.Value.RuntimeStateRoutingInfo = "diag-routing";
            options.Value.DiagnosticsTarget = PublisherDiagnosticTargetType.Events;
            var logger = Log.Console<RuntimeStateReporter>();

            using var reporter = new RuntimeStateReporter(
                client.Object.YieldReturn(),
                new MemoryKVStore().YieldReturn(),
                options,
                collector.Object,
                logger);

            var diagnostics = new List<(string, WriterGroupDiagnosticModel)>
            {
                ("wg", new WriterGroupDiagnosticModel { WriterGroupName = "RoutingGroup" })
            };

            var method = typeof(RuntimeStateReporter).GetMethod(
                "SendDiagnosticsAsync",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            await (ValueTask)method.Invoke(reporter, [
                (IEnumerable<(string, WriterGroupDiagnosticModel)>)diagnostics,
                CancellationToken.None
            ])!;

            Assert.True(capturedProperties.ContainsKey(OpcUa.Constants.MessagePropertyRoutingKey));
        }

        [Fact]
        public async Task WorkloadApiThrowsNotSupportedException_IsHandledGracefullyAsync()
        {
            var client = new Mock<IEventClient>();
            var collector = new Mock<IDiagnosticCollector>();
            var workload = new Mock<IIoTEdgeWorkloadApi>();

            workload.Setup(w => w.CreateServerCertificateAsync(
                    It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .Returns((string _, DateTime _, CancellationToken _) =>
                    new ValueTask<System.Security.Cryptography.X509Certificates.X509Certificate2Collection>(
                        Task.FromException<System.Security.Cryptography.X509Certificates.X509Certificate2Collection>(
                            new NotSupportedException("workload API not supported"))));

            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            var logger = Log.Console<RuntimeStateReporter>();

            using var reporter = new RuntimeStateReporter(
                client.Object.YieldReturn(),
                new MemoryKVStore().YieldReturn(),
                options,
                collector.Object,
                logger,
                workload: workload.Object);

            // Should not throw — NotSupportedException is caught and logged
            await reporter.SendRestartAnnouncementAsync(default);

            // A fallback self-signed certificate should still have been generated
            Assert.NotNull(reporter.Certificate);
        }

        [Fact]
        public async Task WorkloadApiThrowsGenericException_IsHandledGracefullyAsync()
        {
            var client = new Mock<IEventClient>();
            var collector = new Mock<IDiagnosticCollector>();
            var workload = new Mock<IIoTEdgeWorkloadApi>();

            workload.Setup(w => w.CreateServerCertificateAsync(
                    It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .Returns((string _, DateTime _, CancellationToken _) =>
                    new ValueTask<System.Security.Cryptography.X509Certificates.X509Certificate2Collection>(
                        Task.FromException<System.Security.Cryptography.X509Certificates.X509Certificate2Collection>(
                            new InvalidOperationException("unexpected workload failure"))));

            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            var logger = Log.Console<RuntimeStateReporter>();

            using var reporter = new RuntimeStateReporter(
                client.Object.YieldReturn(),
                new MemoryKVStore().YieldReturn(),
                options,
                collector.Object,
                logger,
                workload: workload.Object);

            await reporter.SendRestartAnnouncementAsync(default);

            Assert.NotNull(reporter.Certificate);
        }
    }
}
