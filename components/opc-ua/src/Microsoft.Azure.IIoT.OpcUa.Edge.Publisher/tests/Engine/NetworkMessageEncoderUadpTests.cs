// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Tests.Engine {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Moq;
    using Opc.Ua;
    using Serilog;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;

    public class NetworkMessageEncoderUadpTests {

        private static NetworkMessageEncoder GetEncoder() {
            var loggerMock = new Mock<ILogger>();
            var engineConfigMock = new Mock<IEngineConfiguration>();
            return new NetworkMessageEncoder(loggerMock.Object, engineConfigMock.Object);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task EncodeChunkedUadpMessageTest1(bool encodeBatchFlag) {
            var maxMessageSize = 100;
            var messages = NetworkMessageEncoderTests.GenerateSampleMessages(10, false, MessageEncoding.Uadp);

            var encoder = GetEncoder();
            var networkMessages = await (encodeBatchFlag
                ? encoder.EncodeBatchAsync(messages, maxMessageSize)
                : encoder.EncodeAsync(messages, maxMessageSize)
            );

            Assert.Equal(10, networkMessages.Count());
            Assert.All(networkMessages, m => Assert.True(m.Body.Length <= maxMessageSize));
            Assert.Equal((uint)10, encoder.NotificationsProcessedCount);
            Assert.Equal((uint)0, encoder.NotificationsDroppedCount);
            Assert.Equal((uint)10, encoder.MessagesProcessedCount);
            Assert.Equal(1, encoder.AvgNotificationsPerMessage);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task EncodeChunkedUadpMessageTest2(bool encodeBatchFlag) {
            var maxMessageSize = 100;
            var messages = NetworkMessageEncoderTests.GenerateSampleMessages(100, false, MessageEncoding.Uadp);

            var encoder = GetEncoder();
            var networkMessages = await (encodeBatchFlag
                ? encoder.EncodeBatchAsync(messages, maxMessageSize)
                : encoder.EncodeAsync(messages, maxMessageSize)
            );

            Assert.Equal(100, networkMessages.Count());
            Assert.All(networkMessages, m => Assert.True(m.Body.Length <= maxMessageSize));
            Assert.Equal((uint)100, encoder.NotificationsProcessedCount);
            Assert.Equal((uint)0, encoder.NotificationsDroppedCount);
            Assert.Equal((uint)100, encoder.MessagesProcessedCount);
            Assert.Equal(1, encoder.AvgNotificationsPerMessage);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task EncodeUadpTest(bool encodeBatchFlag) {
            var maxMessageSize = 256 * 1024;
            var messages = NetworkMessageEncoderTests.GenerateSampleMessages(20, false, MessageEncoding.Uadp);

            var encoder = GetEncoder();
            var networkMessages = await (encodeBatchFlag
                ? encoder.EncodeBatchAsync(messages, maxMessageSize)
                : encoder.EncodeAsync(messages, maxMessageSize)
            );

            Assert.Equal(1, networkMessages.Count());
            Assert.Equal((uint)20, encoder.NotificationsProcessedCount);
            Assert.Equal((uint)0, encoder.NotificationsDroppedCount);
            Assert.Equal((uint)1, encoder.MessagesProcessedCount);
            Assert.Equal(20, encoder.AvgNotificationsPerMessage);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task EncodeEventsUadpTest(bool encodeBatchFlag) {
            var maxMessageSize = 256 * 1024;
            var messages = NetworkMessageEncoderTests.GenerateSampleMessages(20, true, MessageEncoding.Uadp);

            var encoder = GetEncoder();
            var networkMessages = await (encodeBatchFlag
                ? encoder.EncodeBatchAsync(messages, maxMessageSize)
                : encoder.EncodeAsync(messages, maxMessageSize)
            );

            Assert.Equal(1, networkMessages.Count());
            Assert.Equal((uint)20, encoder.NotificationsProcessedCount);
            Assert.Equal((uint)0, encoder.NotificationsDroppedCount);
            Assert.Equal((uint)1, encoder.MessagesProcessedCount);
            Assert.Equal(20, encoder.AvgNotificationsPerMessage);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task EncodeMetadataUadpTest(bool encodeBatchFlag) {
            var maxMessageSize = 256 * 1024;
            var messages = NetworkMessageEncoderTests.GenerateSampleMessages(20, false, MessageEncoding.Uadp);
            messages[10].Notifications = null; // Emit metadata
            messages[10].MetaData = new DataSetMetaDataType {
                Name = "test",
                Fields = new FieldMetaDataCollection {
                    new FieldMetaData {
                        Name = "test",
                        BuiltInType = (byte)BuiltInType.UInt16
                    }
                }
            };

            var encoder = GetEncoder();
            var networkMessages = await (encodeBatchFlag
                ? encoder.EncodeBatchAsync(messages, maxMessageSize)
                : encoder.EncodeAsync(messages, maxMessageSize)
            );

            Assert.Equal(3, networkMessages.Count());
            Assert.Equal((uint)19, encoder.NotificationsProcessedCount);
            Assert.Equal((uint)0, encoder.NotificationsDroppedCount);
            Assert.Equal((uint)3, encoder.MessagesProcessedCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task EncodeMetadataUadpChunkTest(bool encodeBatchFlag) {
            var maxMessageSize = 100;
            var messages = NetworkMessageEncoderTests.GenerateSampleMessages(1, false, MessageEncoding.Uadp);
            messages[0].Notifications = null; // Emit metadata
            messages[0].MetaData = new DataSetMetaDataType {
                Name = "test",
                Fields = Enumerable.Range(0, 10000).Select(r =>
                    new FieldMetaData {
                        Name = "testfield" + r,
                        BuiltInType = (byte)BuiltInType.UInt16
                    }).ToArray()
            };

            var encoder = GetEncoder();
            var networkMessages = await (encodeBatchFlag
                ? encoder.EncodeBatchAsync(messages, maxMessageSize)
                : encoder.EncodeAsync(messages, maxMessageSize)
            );

            Assert.Equal(7625, networkMessages.Count());
            Assert.Equal((uint)0, encoder.NotificationsProcessedCount);
            Assert.Equal((uint)0, encoder.NotificationsDroppedCount);
            Assert.Equal((uint)7625, encoder.MessagesProcessedCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task EncodeNothingTest(bool encodeBatchFlag) {
            var maxMessageSize = 256 * 1024;
            var messages = NetworkMessageEncoderTests.GenerateSampleMessages(1, false, MessageEncoding.Uadp);
            messages[0].Notifications = null;
            messages[0].MetaData = null;

            var encoder = GetEncoder();
            var networkMessages = await (encodeBatchFlag
                ? encoder.EncodeBatchAsync(messages, maxMessageSize)
                : encoder.EncodeAsync(messages, maxMessageSize)
            );

            Assert.Empty(networkMessages);
            Assert.Equal((uint)0, encoder.NotificationsProcessedCount);
            Assert.Equal((uint)0, encoder.NotificationsDroppedCount);
            Assert.Equal((uint)0, encoder.MessagesProcessedCount);
            Assert.Equal(0, encoder.AvgNotificationsPerMessage);
        }
    }
}
