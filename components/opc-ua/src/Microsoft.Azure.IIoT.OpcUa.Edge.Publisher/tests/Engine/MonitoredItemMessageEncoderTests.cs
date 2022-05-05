// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Tests.Engine {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Moq;
    using Serilog;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;

    public class MonitoredItemMessageEncoderTests {

        private readonly Mock<ILogger> _loggerMock;
        private readonly Mock<IEngineConfiguration> _engineConfigMock;
        private readonly MonitoredItemMessageEncoder _encoder;

        public MonitoredItemMessageEncoderTests() {
            _loggerMock = new Mock<ILogger>();
            _engineConfigMock = new Mock<IEngineConfiguration>();
            _encoder = new MonitoredItemMessageEncoder(_loggerMock.Object, _engineConfigMock.Object);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task EmptyMessagesTest(bool encodeBatchFlag) {
            var maxMessageSize = 256 * 1024;
            var messages = new List<DataSetMessageModel>();

            var networkMessages = await (encodeBatchFlag
                ? _encoder.EncodeBatchAsync(messages, maxMessageSize, false)
                : _encoder.EncodeAsync(messages, maxMessageSize, false)
            );

            Assert.Empty(networkMessages);
            Assert.Equal((uint)0, _encoder.NotificationsProcessedCount);
            Assert.Equal((uint)0, _encoder.NotificationsDroppedCount);
            Assert.Equal((uint)0, _encoder.MessagesProcessedCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task EmptyDataSetMessageModelTest(bool encodeBatchFlag) {
            var maxMessageSize = 256 * 1024;
            var messages = new[] { new DataSetMessageModel() };

            var networkMessages = await (encodeBatchFlag
                ? _encoder.EncodeBatchAsync(messages, maxMessageSize, false)
                : _encoder.EncodeAsync(messages, maxMessageSize, false)
            );

            Assert.Empty(networkMessages);
            Assert.Equal((uint)0, _encoder.NotificationsProcessedCount);
            Assert.Equal((uint)0, _encoder.NotificationsDroppedCount);
            Assert.Equal((uint)0, _encoder.MessagesProcessedCount);
        }

        [Theory]
        [InlineData(false, MessageEncoding.Json)]
        [InlineData(true, MessageEncoding.Json)]
        [InlineData(false, MessageEncoding.Uadp)]
        [InlineData(true, MessageEncoding.Uadp)]
        public async Task EncodeTooBigMessageTest(bool encodeBatchFlag, MessageEncoding encoding) {
            var maxMessageSize = 100;
            var messages = NetworkMessageEncoderTests.GenerateSampleMessages(3, encoding);

            var networkMessages = await (encodeBatchFlag
                ? _encoder.EncodeBatchAsync(messages, maxMessageSize, false)
                : _encoder.EncodeAsync(messages, maxMessageSize, false)
            );

            Assert.Empty(networkMessages);
            Assert.Equal((uint)0, _encoder.NotificationsProcessedCount);
            Assert.Equal((uint)6, _encoder.NotificationsDroppedCount);
            Assert.Equal((uint)0, _encoder.MessagesProcessedCount);
        }

        [Theory]
        [InlineData(false, MessageEncoding.Json)]
        [InlineData(true, MessageEncoding.Json)]
        [InlineData(false, MessageEncoding.Uadp)]
        [InlineData(true, MessageEncoding.Uadp)]
        public async Task EncodeTest(bool encodeBatchFlag, MessageEncoding encoding) {
            var maxMessageSize = 256 * 1024;
            var messages = NetworkMessageEncoderTests.GenerateSampleMessages(20, encoding);

            var networkMessages = await (encodeBatchFlag
                ? _encoder.EncodeBatchAsync(messages, maxMessageSize, false)
                : _encoder.EncodeAsync(messages, maxMessageSize, false)
            );

            if (encodeBatchFlag) {
                Assert.Equal(1, networkMessages.Count());
                Assert.Equal((uint)210, _encoder.NotificationsProcessedCount);
                Assert.Equal((uint)0, _encoder.NotificationsDroppedCount);
                Assert.Equal((uint)1, _encoder.MessagesProcessedCount);
                Assert.Equal(210, _encoder.AvgNotificationsPerMessage);
            }
            else {
                Assert.Equal(210, networkMessages.Count());
                Assert.Equal((uint)210, _encoder.NotificationsProcessedCount);
                Assert.Equal((uint)0, _encoder.NotificationsDroppedCount);
                Assert.Equal((uint)210, _encoder.MessagesProcessedCount);
                Assert.Equal(1, _encoder.AvgNotificationsPerMessage);
            }
        }
    }
}
