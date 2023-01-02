// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Tests.Engine {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Runtime;
    using Moq;
    using Opc.Ua;
    using Serilog;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;

    public class NetworkMessageEncoderJsonGzipTests {

        /// <summary>
        /// Create compliant encoder
        /// </summary>
        /// <returns></returns>
        private static NetworkMessageEncoder GetEncoder() {
            var loggerMock = new Mock<ILogger>();
            return new NetworkMessageEncoder(loggerMock.Object, new WriterGroupJobConfig {
                UseStandardsCompliantEncoding = true,
            });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task EmptyMessagesTest(bool encodeBatchFlag) {
            var maxMessageSize = 256 * 1024;
            var messages = new List<DataSetMessageModel>();

            var encoder = GetEncoder();
            var networkMessages = await (encodeBatchFlag
                ? encoder.EncodeBatchAsync(messages, maxMessageSize)
                : encoder.EncodeAsync(messages, maxMessageSize)
            );

            Assert.Empty(networkMessages);
            Assert.Equal((uint)0, encoder.NotificationsProcessedCount);
            Assert.Equal((uint)0, encoder.NotificationsDroppedCount);
            Assert.Equal((uint)0, encoder.MessagesProcessedCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task EmptyDataSetMessageModelTest(bool encodeBatchFlag) {
            var maxMessageSize = 256 * 1024;
            var messages = new[] { new DataSetMessageModel() };

            var encoder = GetEncoder();
            var networkMessages = await (encodeBatchFlag
                ? encoder.EncodeBatchAsync(messages, maxMessageSize)
                : encoder.EncodeAsync(messages, maxMessageSize)
            );

            Assert.Empty(networkMessages);
            Assert.Equal((uint)0, encoder.NotificationsProcessedCount);
            Assert.Equal((uint)0, encoder.NotificationsDroppedCount);
            Assert.Equal((uint)0, encoder.MessagesProcessedCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task EncodeTooBigJsonMessageTest(bool encodeBatchFlag) {
            var maxMessageSize = 100;
            var messages = NetworkMessageEncoderTests.GenerateSampleMessages(3, false, MessageEncoding.JsonGzip);

            var encoder = GetEncoder();
            var networkMessages = await (encodeBatchFlag
                ? encoder.EncodeBatchAsync(messages, maxMessageSize)
                : encoder.EncodeAsync(messages, maxMessageSize)
            );

            Assert.Empty(networkMessages);
            Assert.Equal((uint)0, encoder.NotificationsProcessedCount);
            Assert.Equal((uint)3, encoder.NotificationsDroppedCount);
            Assert.Equal((uint)0, encoder.MessagesProcessedCount);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task EncodeJsonTest(bool encodeBatchFlag) {
            var maxMessageSize = 256 * 1024;
            var messages = NetworkMessageEncoderTests.GenerateSampleMessages(20, false, MessageEncoding.JsonGzip);

            var encoder = GetEncoder();
            var networkMessages = await (encodeBatchFlag
                ? encoder.EncodeBatchAsync(messages, maxMessageSize)
                : encoder.EncodeAsync(messages, maxMessageSize)
            );

            // Batch or no batch, the difference is that we cram all notifications
            // into a message or write all messages as array in batch mode. If
            // single message is desired, single message mode should be set (see next test).

            Assert.Equal(1, networkMessages.Count());
            Assert.Equal((uint)20, encoder.NotificationsProcessedCount);
            Assert.Equal((uint)0, encoder.NotificationsDroppedCount);
            Assert.Equal((uint)1, encoder.MessagesProcessedCount);
            Assert.Equal(20, encoder.AvgNotificationsPerMessage);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task EncodeJsonChunkTest(bool encodeBatchFlag) {
            var maxMessageSize = 8 * 1024;
            var messages = NetworkMessageEncoderTests.GenerateSampleMessages(500, false, MessageEncoding.JsonGzip);

            var encoder = GetEncoder();
            var networkMessages = await (encodeBatchFlag
                ? encoder.EncodeBatchAsync(messages, maxMessageSize)
                : encoder.EncodeAsync(messages, maxMessageSize)
            );

            var count = networkMessages.Count();
            var total = networkMessages.Sum(m => m.Body.Length);
            Assert.InRange(count, 167, 187);
            Assert.Equal((uint)500, encoder.NotificationsProcessedCount);
            Assert.Equal((uint)0, encoder.NotificationsDroppedCount);
            Assert.Equal((uint)count, encoder.MessagesProcessedCount);
            Assert.Equal(2, encoder.AvgNotificationsPerMessage);
        }

        [Fact]
        public async Task EncodeJsonSingleMessageTest() {
            var maxMessageSize = 256 * 1024;
            var messages = NetworkMessageEncoderTests.GenerateSampleMessages(20, false, MessageEncoding.JsonGzip,
                NetworkMessageContentMask.SingleDataSetMessage);

            var encoder = GetEncoder();
            var networkMessages = await encoder.EncodeAsync(messages, maxMessageSize);

            Assert.Equal(20, networkMessages.Count());
            Assert.Equal((uint)20, encoder.NotificationsProcessedCount);
            Assert.Equal((uint)0, encoder.NotificationsDroppedCount);
            Assert.Equal((uint)20, encoder.MessagesProcessedCount);
            Assert.Equal(1, encoder.AvgNotificationsPerMessage);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task EncodeEventsJsonTest(bool encodeBatchFlag) {
            var maxMessageSize = 256 * 1024;
            var messages = NetworkMessageEncoderTests.GenerateSampleMessages(20, true, MessageEncoding.JsonGzip);

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

        [Fact]
        public async Task EncodeEventsSingleMessageJsonTest() {
            var maxMessageSize = 256 * 1024;
            var messages = NetworkMessageEncoderTests.GenerateSampleMessages(20, true, MessageEncoding.JsonGzip,
                NetworkMessageContentMask.SingleDataSetMessage);

            var encoder = GetEncoder();
            var networkMessages = await encoder.EncodeAsync(messages, maxMessageSize);

            // Single message, no array envelope due to batching resulting in 210 events from 20 notifications.
            Assert.Equal(210, networkMessages.Count());
            Assert.Equal((uint)20, encoder.NotificationsProcessedCount);
            Assert.Equal((uint)0, encoder.NotificationsDroppedCount);
            Assert.Equal((uint)210, encoder.MessagesProcessedCount);
            Assert.Equal(0, encoder.AvgNotificationsPerMessage);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task EncodeMetadataJsonTest(bool encodeBatchFlag) {
            var maxMessageSize = 256 * 1024;
            var messages = NetworkMessageEncoderTests.GenerateSampleMessages(20, false, MessageEncoding.JsonGzip);
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
        public async Task EncodeNothingTest(bool encodeBatchFlag) {
            var maxMessageSize = 256 * 1024;
            var messages = NetworkMessageEncoderTests.GenerateSampleMessages(1, false, MessageEncoding.JsonGzip);
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
