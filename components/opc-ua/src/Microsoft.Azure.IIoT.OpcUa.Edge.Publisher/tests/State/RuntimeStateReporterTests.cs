// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Tests.State {

    using FluentAssertions;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.State;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    public class RuntimeStateReporterTests {

        [Fact]
        public async Task ReportingDisabledTest() {
            var _client = new Mock<IClient>();
            var _clientAccessorMock = new Mock<IClientAccessor>();
            _clientAccessorMock.Setup(m => m.Client).Returns(_client.Object);

            IJsonSerializer _serializer = new NewtonSoftJsonSerializer();
            var _config = new Mock<IRuntimeStateReporterConfiguration>();
            // This will disable state reporting.
            _config.Setup(c => c.EnableRuntimeStateReporting).Returns(false);

            var _logger = TraceLogger.Create();

            var runtimeStateReporter = new RuntimeStateReporter(
                _clientAccessorMock.Object,
                _serializer,
                _config.Object,
                _logger
            );

            await FluentActions
                .Invoking(async () => await runtimeStateReporter.SendRestartAnnouncement().ConfigureAwait(false))
                .Should()
                .NotThrowAsync()
                .ConfigureAwait(false);

            _client.Verify(c => c.SendEventAsync(It.IsAny<string>(), It.IsAny<Message>()), Times.Never());
        }

        [Fact]
        public async Task ClientNotInitializedTest() {
            var _clientAccessorMock = new Mock<IClientAccessor>();
            _clientAccessorMock.Setup(m => m.Client).Returns((IClient)null);

            IJsonSerializer _serializer = new NewtonSoftJsonSerializer();
            var _config = new Mock<IRuntimeStateReporterConfiguration>();
            _config.Setup(c => c.EnableRuntimeStateReporting).Returns(true);

            var _logger = TraceLogger.Create();

            var runtimeStateReporter = new RuntimeStateReporter(
                _clientAccessorMock.Object,
                _serializer,
                _config.Object,
                _logger
            );

            await runtimeStateReporter.SendRestartAnnouncement().ConfigureAwait(false);

            await FluentActions
                .Invoking(async () => await runtimeStateReporter.SendRestartAnnouncement().ConfigureAwait(false))
                .Should()
                .NotThrowAsync()
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task ReportingTest() {
            var receivedParameters = new List<Tuple<string, Message>>();

            var _client = new Mock<IClient>();
            _client
                .Setup(c => c.SendEventAsync(It.IsAny<string>(), It.IsAny<Message>()))
                .Callback<string, Message>((outputName, message) => receivedParameters.Add(Tuple.Create(outputName, message)))
                .Returns(Task.CompletedTask);

            var _clientAccessorMock = new Mock<IClientAccessor>();
            _clientAccessorMock.Setup(m => m.Client).Returns(_client.Object);

            IJsonSerializer _serializer = new NewtonSoftJsonSerializer();
            var _config = new Mock<IRuntimeStateReporterConfiguration>();
            _config.Setup(c => c.EnableRuntimeStateReporting).Returns(true);

            var _logger = TraceLogger.Create();

            var runtimeStateReporter = new RuntimeStateReporter(
                _clientAccessorMock.Object,
                _serializer,
                _config.Object,
                _logger
            );

            await FluentActions
                .Invoking(async () => await runtimeStateReporter.SendRestartAnnouncement().ConfigureAwait(false))
                .Should()
                .NotThrowAsync()
                .ConfigureAwait(false);

            _client.Verify(c => c.SendEventAsync(It.IsAny<string>(), It.IsAny<Message>()), Times.Once());

            Assert.Equal(1, receivedParameters.Count);
            Assert.Equal("runtimeinfo", receivedParameters[0].Item1);

            var message = receivedParameters[0].Item2;
            Assert.Equal("application/json", message.ContentType);
            Assert.Equal("utf-8", message.ContentEncoding);

            var body = Encoding.UTF8.GetString(message.GetBytes());
            Assert.Equal("{\"MessageType\":\"restartAnnouncement\",\"MessageVersion\":1}", body);
        }
    }
}
