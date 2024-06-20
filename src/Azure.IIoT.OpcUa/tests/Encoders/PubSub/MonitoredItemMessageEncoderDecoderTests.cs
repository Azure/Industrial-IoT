// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub
{
    using Azure.IIoT.OpcUa.Encoders.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    /// <summary>
    /// Monitored item json encoder decoder tests
    /// </summary>
    public class MonitoredItemMessageEncoderDecoderTests
    {
        public const NetworkMessageContentFlags NetworkMessageContentMaskDefault =
            NetworkMessageContentFlags.PublisherId |
            // Important: No NetworkMessageContentFlags.NetworkMessageHeader |
            NetworkMessageContentFlags.DataSetMessageHeader |
            NetworkMessageContentFlags.DataSetClassId;

        public const DataSetMessageContentFlags DataSetMessageContentMaskDefault =
            DataSetMessageContentFlags.MessageType |
            DataSetMessageContentFlags.DataSetWriterId |
            DataSetMessageContentFlags.SequenceNumber |
            DataSetMessageContentFlags.MetaDataVersion |
            DataSetMessageContentFlags.Timestamp |
            DataSetMessageContentFlags.Status;

        public const DataSetFieldContentFlags DataSetFieldContentFlagsDefault =
            DataSetFieldContentFlags.ApplicationUri | // Important
            DataSetFieldContentFlags.EndpointUrl | // Important
            DataSetFieldContentFlags.NodeId | // Important
            DataSetFieldContentFlags.DisplayName | // Important
            DataSetFieldContentFlags.SourceTimestamp |
            DataSetFieldContentFlags.ServerTimestamp |
            DataSetFieldContentFlags.SourcePicoSeconds |
            DataSetFieldContentFlags.ServerPicoSeconds |
            DataSetFieldContentFlags.StatusCode;

        [Theory]
        [InlineData(false, false, false, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 1)]
        [InlineData(false, false, false, NetworkMessageContentMaskDefault, 3)]
        [InlineData(false, false, false, NetworkMessageContentMaskDefault, 1)]
        [InlineData(false, false, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 1)]
        [InlineData(false, false, true, NetworkMessageContentMaskDefault, 3)]
        [InlineData(true, false, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 1)]
        [InlineData(true, false, true, NetworkMessageContentMaskDefault, 3)]
        [InlineData(false, true, false, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 1)]
        [InlineData(false, true, false, NetworkMessageContentMaskDefault, 3)]
        [InlineData(false, true, false, NetworkMessageContentMaskDefault, 1)]
        [InlineData(false, true, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 1)]
        [InlineData(false, true, true, NetworkMessageContentMaskDefault, 3)]
        [InlineData(true, true, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 1)]
        [InlineData(true, true, true, NetworkMessageContentMaskDefault, 3)]
        public void EncodeDecodeNetworkMessage(bool useArrayEnvelope, bool compress,
            bool useCompatibilityMode, NetworkMessageContentFlags contentMask, int numberOfMessages)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(useCompatibilityMode, sequenceNumber))
                .ToList();
            var networkMessage = CreateNetworkMessage(contentMask, messages);
            networkMessage.UseGzipCompression = compress;
            networkMessage.UseArrayEnvelope = useArrayEnvelope;

            var context = new ServiceMessageContext();
            var buffer = Assert.Single(networkMessage.Encode(context, 256 * 1000));

            ConvertToOpcUaUniversalTime(networkMessage);

            var result = ((BaseNetworkMessage)PubSubMessage
                   .Decode(buffer, networkMessage.ContentType, context, messageSchema: networkMessage.MessageSchema)).Messages;
            Assert.Equal(messages, result);
        }

        [Theory]
        [InlineData(false, false, false, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 1)]
        [InlineData(false, false, false, NetworkMessageContentMaskDefault, 3)]
        [InlineData(false, false, false, NetworkMessageContentMaskDefault, 1)]
        [InlineData(false, false, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 1)]
        [InlineData(false, false, true, NetworkMessageContentMaskDefault, 3)]
        [InlineData(true, false, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 1)]
        [InlineData(true, false, true, NetworkMessageContentMaskDefault, 3)]
        [InlineData(false, true, false, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 1)]
        [InlineData(false, true, false, NetworkMessageContentMaskDefault, 3)]
        [InlineData(false, true, false, NetworkMessageContentMaskDefault, 1)]
        [InlineData(false, true, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 1)]
        [InlineData(false, true, true, NetworkMessageContentMaskDefault, 3)]
        [InlineData(true, true, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 1)]
        [InlineData(true, true, true, NetworkMessageContentMaskDefault, 3)]
        public void EncodeDecodeNetworkMessageReversible(bool useArrayEnvelope, bool compress,
            bool useCompatibilityMode, NetworkMessageContentFlags contentMask, int numberOfMessages)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(useCompatibilityMode, sequenceNumber,
                    DataSetMessageContentMaskDefault | DataSetMessageContentFlags.ReversibleFieldEncoding))
                .ToList();
            var networkMessage = CreateNetworkMessage(contentMask, messages);
            networkMessage.UseGzipCompression = compress;
            networkMessage.UseArrayEnvelope = useArrayEnvelope;

            var context = new ServiceMessageContext();
            var buffer = Assert.Single(networkMessage.Encode(context, 256 * 1000));

            ConvertToOpcUaUniversalTime(networkMessage);

            var result = ((BaseNetworkMessage)PubSubMessage
                   .Decode(buffer, networkMessage.ContentType, context, messageSchema: networkMessage.MessageSchema)).Messages;
            Assert.Equal(messages, result);
        }

        [Theory]
        [InlineData(false, false, false, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, false, false, NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(false, false, true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(false, false, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(true, false, true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(true, false, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, false, false, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 1024)]
        [InlineData(false, false, false, NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(false, false, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(false, false, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 1024)]
        [InlineData(true, false, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(true, false, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 1024)]
        [InlineData(false, true, false, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, true, false, NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(false, true, true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(false, true, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(true, true, true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(true, true, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, true, false, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 1024)]
        [InlineData(false, true, false, NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(false, true, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(false, true, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 1024)]
        [InlineData(true, true, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(true, true, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 1024)]
        public void EncodeDecodeNetworkMessages(bool useArrayEnvelope, bool compress,
            bool useCompatibilityMode, NetworkMessageContentFlags contentMask, int numberOfMessages, int maxMessageSize)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(useCompatibilityMode, sequenceNumber))
                .ToList();
            var networkMessage = CreateNetworkMessage(contentMask, messages);
            networkMessage.UseGzipCompression = compress;
            networkMessage.UseArrayEnvelope = useArrayEnvelope;

            var context = new ServiceMessageContext();
            var buffers = networkMessage.Encode(context, maxMessageSize);

            ConvertToOpcUaUniversalTime(networkMessage);

            var result = buffers
                .Select(buffer => (BaseNetworkMessage)PubSubMessage
                    .Decode(buffer, networkMessage.ContentType, context, messageSchema: networkMessage.MessageSchema))
                .SelectMany(m => m.Messages).ToList();
            Assert.Equal(messages, result);
        }

        [Theory]
        [InlineData(false, false, false, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, false, false, NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(false, false, true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(false, false, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(true, false, true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(true, false, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, false, false, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 1024)]
        [InlineData(false, false, false, NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(false, false, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(false, false, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 1024)]
        [InlineData(true, false, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(true, false, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 1024)]
        [InlineData(false, true, false, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, true, false, NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(false, true, true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(false, true, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(true, true, true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(true, true, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, true, false, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 1024)]
        [InlineData(false, true, false, NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(false, true, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(false, true, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 1024)]
        [InlineData(true, true, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(true, true, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 1024)]
        public void EncodeDecodeNetworkMessagesReversible(bool useArrayEnvelope, bool compress,
            bool useCompatibilityMode, NetworkMessageContentFlags contentMask, int numberOfMessages, int maxMessageSize)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(useCompatibilityMode, sequenceNumber,
                    DataSetMessageContentMaskDefault | DataSetMessageContentFlags.ReversibleFieldEncoding))
                .ToList();
            var networkMessage = CreateNetworkMessage(contentMask, messages);
            networkMessage.UseGzipCompression = compress;
            networkMessage.UseArrayEnvelope = useArrayEnvelope;

            var context = new ServiceMessageContext();
            var buffers = networkMessage.Encode(context, maxMessageSize);

            ConvertToOpcUaUniversalTime(networkMessage);

            var result = buffers
                .Select(buffer => (BaseNetworkMessage)PubSubMessage
                    .Decode(buffer, networkMessage.ContentType, context, messageSchema: networkMessage.MessageSchema))
                .SelectMany(m => m.Messages).ToList();
            Assert.Equal(messages, result);
        }

        /// <summary>
        /// Convert timestamps of payload to OpcUa Utc.
        /// </summary>
        /// <param name="networkMessage"></param>
        private static void ConvertToOpcUaUniversalTime(BaseNetworkMessage networkMessage)
        {
            // convert DataSet Payload DataValue timestamps to OpcUa Utc
            foreach (var dataSetMessage in networkMessage.Messages)
            {
                var expectedPayload = new Dictionary<string, DataValue>();
                foreach (var entry in dataSetMessage.Payload)
                {
                    expectedPayload[entry.Key] = new DataValue(entry.Value).ToOpcUaUniversalTime();
                }
                dataSetMessage.Payload = new DataSet(expectedPayload,
                    DataSetFieldContentFlags.ApplicationUri | // Important
                    DataSetFieldContentFlags.EndpointUrl | // Important
                    DataSetFieldContentFlags.NodeId | // Important
                    DataSetFieldContentFlags.DisplayName | // Important
                    DataSetFieldContentFlags.StatusCode |
                    DataSetFieldContentFlags.SourceTimestamp);
            }
        }

        /// <summary>
        /// Create network message
        /// </summary>
        /// <param name="contentMask"></param>
        /// <param name="messages"></param>
        private static JsonNetworkMessage CreateNetworkMessage(
            NetworkMessageContentFlags contentMask, List<BaseDataSetMessage> messages)
        {
            return new JsonNetworkMessage
            {
                MessageId = () => "9279C0B3-DA88-45A4-AF74-451CEBF82DB0",
                Messages = messages,
                DataSetWriterGroup = "group",
                DataSetClassId = Guid.NewGuid(),
                PublisherId = "PublisherId",
                NetworkMessageContentMask = contentMask
            };
        }

        /// <summary>
        /// Create dataset message
        /// </summary>
        /// <param name="useCompatibilityMode"></param>
        /// <param name="sequenceNumber"></param>
        /// <param name="dataSetMessageContentMask"></param>
        /// <param name="dataSetFieldContentMask"></param>
        private static MonitoredItemMessage CreateDataSetMessage(bool useCompatibilityMode, int sequenceNumber,
            DataSetMessageContentFlags dataSetMessageContentMask = DataSetMessageContentMaskDefault,
            DataSetFieldContentFlags dataSetFieldContentMask = DataSetFieldContentFlagsDefault)
        {
            return new MonitoredItemMessage
            {
                EndpointUrl = "EndpointUrl",
                ApplicationUri = "ApplicationUrl",
                NodeId = "NodeId",
                SequenceNumber = (ushort)sequenceNumber,
                UseCompatibilityMode = useCompatibilityMode,
                Status = StatusCodes.Bad,
                Timestamp = DateTimeOffset.UtcNow,
                Payload = CreateDataSet(dataSetFieldContentMask),
                DataSetMessageContentMask = dataSetMessageContentMask
            };
        }

        /// <summary>
        /// Create dataset
        /// </summary>
        /// <param name="dataSetFieldContentMask"></param>
        private static DataSet CreateDataSet(DataSetFieldContentFlags dataSetFieldContentMask = DataSetFieldContentFlagsDefault)
        {
            return new DataSet(new Dictionary<string, DataValue>
            {
                { "1", new DataValue(new Variant("test"), StatusCodes.Good, DateTime.Now, DateTime.UtcNow) }
            }, dataSetFieldContentMask);
        }
    }
}
