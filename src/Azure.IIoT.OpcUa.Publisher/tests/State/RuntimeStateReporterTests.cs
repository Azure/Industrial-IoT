// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.State
{
    using Azure.IIoT.OpcUa.Publisher.State;
    using FluentAssertions;
    using Furly.Extensions.Logging;
    using Furly.Extensions.Messaging;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Microsoft.Azure.IIoT.Messaging;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class RuntimeStateReporterTests
    {
        [Fact]
        public async Task ReportingDisabledTest()
        {
            var _client = new Mock<IEventClient>();

            IJsonSerializer _serializer = new NewtonsoftJsonSerializer();
            var _config = new Mock<IRuntimeStateReporterConfiguration>();
            // This will disable state reporting.
            _config.Setup(c => c.EnableRuntimeStateReporting).Returns(false);

            var _logger = Log.Console<RuntimeStateReporter>();

            var runtimeStateReporter = new RuntimeStateReporter(
                _client.Object,
                _serializer,
                _config.Object,
                _logger
            );

            await FluentActions
                .Invoking(async () => await runtimeStateReporter.SendRestartAnnouncementAsync().ConfigureAwait(false))
                .Should()
                .NotThrowAsync()
                .ConfigureAwait(false);

            _client.Verify(c => c.CreateEvent(), Times.Never());
        }

        [Fact]
        public async Task ClientNotInitializedTest()
        {
            var _clientAccessorMock = new Mock<IEventClient>();
            _clientAccessorMock.Setup(m => m.CreateEvent()).Throws<IOException>();

            IJsonSerializer _serializer = new NewtonsoftJsonSerializer();
            var _config = new Mock<IRuntimeStateReporterConfiguration>();
            _config.Setup(c => c.EnableRuntimeStateReporting).Returns(true);

            var _logger = Log.Console<RuntimeStateReporter>();

            var runtimeStateReporter = new RuntimeStateReporter(
                _clientAccessorMock.Object,
                _serializer,
                _config.Object,
                _logger
            );

            await runtimeStateReporter.SendRestartAnnouncementAsync().ConfigureAwait(false);

            await FluentActions
                .Invoking(async () => await runtimeStateReporter.SendRestartAnnouncementAsync().ConfigureAwait(false))
                .Should()
                .NotThrowAsync()
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task ReportingTest()
        {
            var _client = new Mock<IEventClient>();

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
            IReadOnlyList<ReadOnlyMemory<byte>> buffers = null;
            _message.Setup(c => c.AddProperty(It.Is<string>(v => v == "RoutingInfo"), It.IsAny<string>()))
                .Callback<string>(v => routingInfo = v)
                .Returns(_message.Object);
            _message.Setup(c => c.SetContentType(It.IsAny<string>()))
                .Callback<string>(v => contentType = v)
                .Returns(_message.Object);
            _message.Setup(c => c.SetContentEncoding(It.IsAny<string>()))
                .Callback<string>(v => contentEncoding = v)
                .Returns(_message.Object);
            _message.Setup(c => c.AddBuffers(It.IsAny<IReadOnlyList<ReadOnlyMemory<byte>>>()))
                .Callback<IReadOnlyList<ReadOnlyMemory<byte>>>(v => buffers = v)
                .Returns(_message.Object);

            IJsonSerializer _serializer = new NewtonsoftJsonSerializer();
            var _config = new Mock<IRuntimeStateReporterConfiguration>();
            _config.Setup(c => c.EnableRuntimeStateReporting).Returns(true);

            var _logger = Log.Console<RuntimeStateReporter>();

            var runtimeStateReporter = new RuntimeStateReporter(
                _client.Object,
                _serializer,
                _config.Object,
                _logger
            );

            await FluentActions
                .Invoking(async () => await runtimeStateReporter.SendRestartAnnouncementAsync().ConfigureAwait(false))
                .Should()
                .NotThrowAsync()
                .ConfigureAwait(false);

            _message.Verify(c => c.SendAsync(It.IsAny<CancellationToken>()), Times.Once());
            _message.Verify(m => m.Dispose(), Times.Once());

            Assert.Equal("runtimeinfo", routingInfo);
            Assert.Equal("application/json", contentType);
            Assert.Equal("utf-8", contentEncoding);

            Assert.Equal(1, buffers.Count);
            var body = Encoding.UTF8.GetString(buffers[0].Span);
            Assert.Equal("{\"MessageType\":\"restartAnnouncement\",\"MessageVersion\":1}", body);
        }
    }
}
