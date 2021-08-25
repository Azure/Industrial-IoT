// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Tests.Engine {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Moq;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    public class MonitoredItemMessageEncoderTests {
        private readonly MonitoredItemMessageEncoder _encoder;

        public MonitoredItemMessageEncoderTests() {
            _encoder = new MonitoredItemMessageEncoder();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EmptyMessagesTest(bool encodeBatchFlag) {
            var maxMessageSize = 256 * 1024;
            var messages = new List<DataSetMessageModel>();

            var networkMessages = (encodeBatchFlag
                ? _encoder.EncodeBatchAsync(messages, maxMessageSize)
                : _encoder.EncodeAsync(messages, maxMessageSize)
            ).ConfigureAwait(false).GetAwaiter().GetResult();

            Assert.Empty(networkMessages);
            Assert.Equal((uint)0, _encoder.NotificationsProcessedCount);
            Assert.Equal((uint)0, _encoder.NotificationsDroppedCount);
            Assert.Equal((uint)0, _encoder.MessagesProcessedCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EmptyDataSetMessageModelTest(bool encodeBatchFlag) {
            var maxMessageSize = 256 * 1024;
            var messages = new[] { new DataSetMessageModel() };

            var networkMessages = (encodeBatchFlag
                ? _encoder.EncodeBatchAsync(messages, maxMessageSize)
                : _encoder.EncodeAsync(messages, maxMessageSize)
            ).ConfigureAwait(false).GetAwaiter().GetResult();

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
        public void EncodeTooBigMessageTest(bool encodeBatchFlag, MessageEncoding encoding) {
            var maxMessageSize = 100;
            var messages = NetworkMessageEncoderTests.GenerateSampleMessages(3, encoding);

            var networkMessages = (encodeBatchFlag
                ? _encoder.EncodeBatchAsync(messages, maxMessageSize)
                : _encoder.EncodeAsync(messages, maxMessageSize)
            ).ConfigureAwait(false).GetAwaiter().GetResult().ToList();

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
        public void EncodeTest(bool encodeBatchFlag, MessageEncoding encoding) {
            var maxMessageSize = 256 * 1024;
            var messages = NetworkMessageEncoderTests.GenerateSampleMessages(20, encoding);

            var networkMessages = (encodeBatchFlag
                ? _encoder.EncodeBatchAsync(messages, maxMessageSize)
                : _encoder.EncodeAsync(messages, maxMessageSize)
            ).ConfigureAwait(false).GetAwaiter().GetResult().ToList();

            if (encodeBatchFlag) {
                Assert.Equal(1, networkMessages.Count);
                Assert.Equal((uint)210, _encoder.NotificationsProcessedCount);
                Assert.Equal((uint)0, _encoder.NotificationsDroppedCount);
                Assert.Equal((uint)1, _encoder.MessagesProcessedCount);
                Assert.Equal(210, _encoder.AvgNotificationsPerMessage);
            }
            else {
                Assert.Equal(210, networkMessages.Count);
                Assert.Equal((uint)210, _encoder.NotificationsProcessedCount);
                Assert.Equal((uint)0, _encoder.NotificationsDroppedCount);
                Assert.Equal((uint)210, _encoder.MessagesProcessedCount);
                Assert.Equal(1, _encoder.AvgNotificationsPerMessage);
            }
        }
    }
}
