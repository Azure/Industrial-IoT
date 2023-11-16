// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub.Tests
{
    using Azure.IIoT.OpcUa.Encoders.Models;
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
        public const JsonNetworkMessageContentMask NetworkMessageContentMaskDefault =
            JsonNetworkMessageContentMask.PublisherId |
            // Important: No JsonNetworkMessageContentMask.NetworkMessageHeader |
            JsonNetworkMessageContentMask.DataSetMessageHeader |
            JsonNetworkMessageContentMask.DataSetClassId;

        public const JsonDataSetMessageContentMask DataSetMessageContentMaskDefault =
            JsonDataSetMessageContentMaskEx.ApplicationUri | // Important
            JsonDataSetMessageContentMaskEx.EndpointUrl | // Important
            JsonDataSetMessageContentMaskEx.NodeId | // Important
            JsonDataSetMessageContentMaskEx.DisplayName | // Important
            JsonDataSetMessageContentMask.MessageType |
            JsonDataSetMessageContentMask.DataSetWriterId |
            JsonDataSetMessageContentMask.SequenceNumber |
            JsonDataSetMessageContentMask.MetaDataVersion |
            JsonDataSetMessageContentMask.Timestamp |
            JsonDataSetMessageContentMask.Status;

        public const DataSetFieldContentMask DataSetFieldContentMaskDefault =
            DataSetFieldContentMask.SourceTimestamp |
            DataSetFieldContentMask.ServerTimestamp |
            DataSetFieldContentMask.SourcePicoSeconds |
            DataSetFieldContentMask.ServerPicoSeconds |
            DataSetFieldContentMask.StatusCode;

        [Theory]
        [InlineData(false, false, false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 1)]
        [InlineData(false, false, false, NetworkMessageContentMaskDefault, 3)]
        [InlineData(false, false, false, NetworkMessageContentMaskDefault, 1)]
        [InlineData(false, false, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 1)]
        [InlineData(false, false, true, NetworkMessageContentMaskDefault, 3)]
        [InlineData(true, false, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 1)]
        [InlineData(true, false, true, NetworkMessageContentMaskDefault, 3)]
        [InlineData(false, true, false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 1)]
        [InlineData(false, true, false, NetworkMessageContentMaskDefault, 3)]
        [InlineData(false, true, false, NetworkMessageContentMaskDefault, 1)]
        [InlineData(false, true, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 1)]
        [InlineData(false, true, true, NetworkMessageContentMaskDefault, 3)]
        [InlineData(true, true, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 1)]
        [InlineData(true, true, true, NetworkMessageContentMaskDefault, 3)]
        public void EncodeDecodeNetworkMessage(bool useArrayEnvelope, bool compress,
            bool useCompatibilityMode, JsonNetworkMessageContentMask contentMask, int numberOfMessages)
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
        [InlineData(false, false, false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 1)]
        [InlineData(false, false, false, NetworkMessageContentMaskDefault, 3)]
        [InlineData(false, false, false, NetworkMessageContentMaskDefault, 1)]
        [InlineData(false, false, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 1)]
        [InlineData(false, false, true, NetworkMessageContentMaskDefault, 3)]
        [InlineData(true, false, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 1)]
        [InlineData(true, false, true, NetworkMessageContentMaskDefault, 3)]
        [InlineData(false, true, false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 1)]
        [InlineData(false, true, false, NetworkMessageContentMaskDefault, 3)]
        [InlineData(false, true, false, NetworkMessageContentMaskDefault, 1)]
        [InlineData(false, true, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 1)]
        [InlineData(false, true, true, NetworkMessageContentMaskDefault, 3)]
        [InlineData(true, true, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 1)]
        [InlineData(true, true, true, NetworkMessageContentMaskDefault, 3)]
        public void EncodeDecodeNetworkMessageReversible(bool useArrayEnvelope, bool compress,
            bool useCompatibilityMode, JsonNetworkMessageContentMask contentMask, int numberOfMessages)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(useCompatibilityMode, sequenceNumber,
                    DataSetMessageContentMaskDefault | JsonDataSetMessageContentMask2.ReversibleFieldEncoding))
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
        [InlineData(false, false, false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, false, false, NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(false, false, true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(false, false, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(true, false, true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(true, false, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, false, false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        [InlineData(false, false, false, NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(false, false, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(false, false, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        [InlineData(true, false, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(true, false, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        [InlineData(false, true, false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, true, false, NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(false, true, true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(false, true, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(true, true, true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(true, true, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, true, false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        [InlineData(false, true, false, NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(false, true, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(false, true, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        [InlineData(true, true, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(true, true, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        public void EncodeDecodeNetworkMessages(bool useArrayEnvelope, bool compress,
            bool useCompatibilityMode, JsonNetworkMessageContentMask contentMask, int numberOfMessages, int maxMessageSize)
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
        [InlineData(false, false, false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, false, false, NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(false, false, true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(false, false, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(true, false, true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(true, false, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, false, false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        [InlineData(false, false, false, NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(false, false, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(false, false, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        [InlineData(true, false, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(true, false, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        [InlineData(false, true, false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, true, false, NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(false, true, true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(false, true, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(true, true, true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(true, true, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, true, false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        [InlineData(false, true, false, NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(false, true, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(false, true, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        [InlineData(true, true, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(true, true, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        public void EncodeDecodeNetworkMessagesReversible(bool useArrayEnvelope, bool compress,
            bool useCompatibilityMode, JsonNetworkMessageContentMask contentMask, int numberOfMessages, int maxMessageSize)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(useCompatibilityMode, sequenceNumber,
                    DataSetMessageContentMaskDefault | JsonDataSetMessageContentMask2.ReversibleFieldEncoding))
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
                dataSetMessage.Payload = new DataSet(expectedPayload, (uint)(
                    DataSetFieldContentMask.StatusCode |
                    DataSetFieldContentMask.SourceTimestamp));
            }
        }

        /// <summary>
        /// Create network message
        /// </summary>
        /// <param name="contentMask"></param>
        /// <param name="messages"></param>
        private static JsonNetworkMessage CreateNetworkMessage(
            JsonNetworkMessageContentMask contentMask, List<BaseDataSetMessage> messages)
        {
            return new JsonNetworkMessage
            {
                MessageId = () => "9279C0B3-DA88-45A4-AF74-451CEBF82DB0",
                Messages = messages,
                DataSetWriterGroup = "group",
                DataSetClassId = Guid.NewGuid(),
                PublisherId = "PublisherId",
                NetworkMessageContentMask = (uint)contentMask
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
            JsonDataSetMessageContentMask dataSetMessageContentMask = DataSetMessageContentMaskDefault,
            DataSetFieldContentMask dataSetFieldContentMask = DataSetFieldContentMaskDefault)
        {
            return new MonitoredItemMessage
            {
                EndpointUrl = "EndpointUrl",
                ApplicationUri = "ApplicationUrl",
                NodeId = "NodeId",
                SequenceNumber = (ushort)sequenceNumber,
                Status = StatusCodes.Bad,
                Timestamp = DateTime.UtcNow,
                Payload = CreateDataSet(dataSetFieldContentMask),
                DataSetMessageContentMask = (uint)dataSetMessageContentMask
            };
        }

        /// <summary>
        /// Create dataset
        /// </summary>
        /// <param name="dataSetFieldContentMask"></param>
        private static DataSet CreateDataSet(DataSetFieldContentMask dataSetFieldContentMask = DataSetFieldContentMaskDefault)
        {
            return new DataSet(new Dictionary<string, DataValue> {
                { "1", new DataValue(new Variant(5), StatusCodes.Good, DateTime.Now, DateTime.UtcNow) }
            }, (uint)dataSetFieldContentMask);
        }
    }
}
