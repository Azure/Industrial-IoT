// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services
{
    using Azure.IIoT.OpcUa.Publisher.Services;
    using FluentAssertions;
    using Furly;
    using Furly.Extensions.Logging;
    using Furly.Extensions.Messaging;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Furly.Extensions.Storage.Services;
    using Microsoft.Extensions.Configuration;
    using Moq;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
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

            IJsonSerializer _serializer = new NewtonsoftJsonSerializer();
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            // This will disable state reporting.
            options.Value.EnableRuntimeStateReporting = false;

            var _logger = Log.Console<RuntimeStateReporter>();

            using var runtimeStateReporter = new RuntimeStateReporter(
                client.Object.YieldReturn(),
                _serializer,
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

            IJsonSerializer _serializer = new NewtonsoftJsonSerializer();
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.EnableRuntimeStateReporting = true;

            var _logger = Log.Console<RuntimeStateReporter>();

            using var runtimeStateReporter = new RuntimeStateReporter(
                client.Object.YieldReturn(),
                _serializer,
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
            IReadOnlyList<ReadOnlySequence<byte>> buffers = null;
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

            IJsonSerializer _serializer = new NewtonsoftJsonSerializer();
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            options.Value.EnableRuntimeStateReporting = true;
            options.Value.RuntimeStateRoutingInfo = "runtimeinfo";

            var _logger = Log.Console<RuntimeStateReporter>();

            using var runtimeStateReporter = new RuntimeStateReporter(
                _client.Object.YieldReturn(),
                _serializer,
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
    }
}
