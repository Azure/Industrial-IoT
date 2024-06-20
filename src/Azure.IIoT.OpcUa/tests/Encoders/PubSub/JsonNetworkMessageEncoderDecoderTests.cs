// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub
{
    using Azure.IIoT.OpcUa.Encoders.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    /// <summary>
    /// Json encoder decoder tests
    /// </summary>
    public class JsonNetworkMessageEncoderDecoderTests
    {
        public const NetworkMessageContentFlags NetworkMessageContentMaskDefault =
            NetworkMessageContentFlags.PublisherId |
            NetworkMessageContentFlags.NetworkMessageHeader |
            NetworkMessageContentFlags.DataSetMessageHeader |
            NetworkMessageContentFlags.DataSetClassId;

        public const DataSetMessageContentFlags DataSetMessageContentMaskDefault =
            DataSetMessageContentFlags.DataSetWriterName |
            DataSetMessageContentFlags.MessageType |
            DataSetMessageContentFlags.DataSetWriterId |
            DataSetMessageContentFlags.SequenceNumber |
            DataSetMessageContentFlags.MetaDataVersion |
            DataSetMessageContentFlags.Timestamp |
            DataSetMessageContentFlags.Status;

        public const DataSetFieldContentFlags DataSetFieldContentFlagsDefault =
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

            var result = PubSubMessage.Decode(buffer, networkMessage.ContentType, context);
            Assert.Equal(networkMessage, result);
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

            var result = PubSubMessage.Decode(buffer, networkMessage.ContentType, context);
            Assert.Equal(networkMessage, result);
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

            var m = buffers
                .Select(buffer => (BaseNetworkMessage)PubSubMessage
                    .Decode(buffer, networkMessage.ContentType, context))
                .ToList();
            var result = m[0];
            result.Messages = m.SelectMany(m => m.Messages).ToList();
            Assert.Equal(networkMessage, result);
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

            var m = buffers
                .Select(buffer => (BaseNetworkMessage)PubSubMessage
                    .Decode(buffer, networkMessage.ContentType, context))
                .ToList();
            var result = m[0];
            result.Messages = m.SelectMany(m => m.Messages).ToList();
            Assert.Equal(networkMessage, result);
        }

        [Theory]
        [InlineData(false, false, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, false, NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, false, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 1024)]
        [InlineData(false, false, NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 1024)]
        public void EncodeDecodeNetworkMessagesNoNetworkMessageHeader(bool useArrayEnvelope,
            bool useCompatibilityMode, NetworkMessageContentFlags contentMask, int numberOfMessages, int maxMessageSize)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(useCompatibilityMode, sequenceNumber))
                .ToList();
            var networkMessage = CreateNetworkMessage(contentMask & ~NetworkMessageContentFlags.NetworkMessageHeader, messages);
            networkMessage.UseArrayEnvelope = useArrayEnvelope;

            var context = new ServiceMessageContext();
            var buffers = networkMessage.Encode(context, maxMessageSize);

            ConvertToOpcUaUniversalTime(networkMessage);

            var result = buffers
                .SelectMany(buffer => ((BaseNetworkMessage)PubSubMessage
                    .Decode(buffer, networkMessage.ContentType, context)).Messages)
                .ToList();
            Assert.Equal(networkMessage.Messages, result);
        }

        [Theory]
        [InlineData(false, false, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, false, NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, false, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 1024)]
        [InlineData(false, false, NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 1024)]
        public void EncodeDecodeNetworkMessagesNoDataSetMessageHeader(bool useArrayEnvelope,
            bool useCompatibilityMode, NetworkMessageContentFlags contentMask, int numberOfMessages, int maxMessageSize)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(useCompatibilityMode, sequenceNumber))
                .ToList();
            var networkMessage = CreateNetworkMessage(contentMask
                & ~NetworkMessageContentFlags.DataSetMessageHeader, messages);
            networkMessage.UseArrayEnvelope = useArrayEnvelope;

            var context = new ServiceMessageContext();
            var buffers = networkMessage.Encode(context, maxMessageSize);

            ConvertToOpcUaUniversalTime(networkMessage);

            var result = buffers
                .SelectMany(buffer => ((BaseNetworkMessage)PubSubMessage
                    .Decode(buffer, networkMessage.ContentType, context)).Messages)
                .Select(m => m.Payload)
                .ToList();
            Assert.All(networkMessage.Messages.Select(m => m.Payload), (p, i) => Assert.True(result[i].Equals(p)));
        }

        [Theory]
        [InlineData(false, false, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, false, NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, false, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 1024)]
        [InlineData(false, false, NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 1024)]
        public void EncodeDecodeNetworkMessagesNoHeader(bool useArrayEnvelope,
            bool useCompatibilityMode, NetworkMessageContentFlags contentMask, int numberOfMessages, int maxMessageSize)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(useCompatibilityMode, sequenceNumber))
                .ToList();
            var networkMessage = CreateNetworkMessage(contentMask
                & ~(NetworkMessageContentFlags.NetworkMessageHeader | NetworkMessageContentFlags.DataSetMessageHeader), messages);
            networkMessage.UseArrayEnvelope = useArrayEnvelope;

            var context = new ServiceMessageContext();
            var buffers = networkMessage.Encode(context, maxMessageSize);

            ConvertToOpcUaUniversalTime(networkMessage);

            var result = buffers
                .SelectMany(buffer => ((BaseNetworkMessage)PubSubMessage
                    .Decode(buffer, networkMessage.ContentType, context)).Messages)
                .Select(m => m.Payload)
                .ToList();
            Assert.All(networkMessage.Messages.Select(m => m.Payload), (p, i) => Assert.True(result[i].Equals(p)));
        }

        [Theory]
        [InlineData(false, false, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, false, NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, false, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 1024)]
        [InlineData(false, false, NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 1024)]
        public void EncodeDecodeNetworkMessagesNoHeaderRaw(bool useArrayEnvelope,
            bool useCompatibilityMode, NetworkMessageContentFlags contentMask, int numberOfMessages, int maxMessageSize)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(useCompatibilityMode, sequenceNumber,
                    dataSetFieldContentMask: DataSetFieldContentFlags.RawData))
                .ToList();
            var networkMessage = CreateNetworkMessage(contentMask
                & ~(NetworkMessageContentFlags.NetworkMessageHeader | NetworkMessageContentFlags.DataSetMessageHeader), messages);
            networkMessage.UseArrayEnvelope = useArrayEnvelope;

            var context = new ServiceMessageContext();
            var buffers = networkMessage.Encode(context, maxMessageSize);

            ConvertToOpcUaUniversalTime(networkMessage);

            // Compare payload as raw data equivalent
            var serializer = new NewtonsoftJsonSerializer();
            var result = serializer.Parse(serializer.SerializeToString(buffers
                .SelectMany(buffer => ((BaseNetworkMessage)PubSubMessage
                    .Decode(buffer, networkMessage.ContentType, context)).Messages)
                .SelectMany(m => m.Payload)
                .Select(v => (v.Key, v.Value.Value))
                .ToList()));
            var expected = serializer.Parse(serializer.SerializeToString(messages
                .SelectMany(m => m.Payload)
                .Select(v => (v.Key, v.Value.Value))
                .ToList()));

            Assert.Equal(expected, result);
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
        private static JsonDataSetMessage CreateDataSetMessage(bool useCompatibilityMode, int sequenceNumber,
            DataSetMessageContentFlags dataSetMessageContentMask = DataSetMessageContentMaskDefault,
            DataSetFieldContentFlags dataSetFieldContentMask = DataSetFieldContentFlagsDefault)
        {
            return new JsonDataSetMessage
            {
                DataSetWriterName = "WriterId",
                DataSetWriterId = (ushort)(useCompatibilityMode ? 0 : 3),
                MetaDataVersion = new ConfigurationVersionDataType { MajorVersion = 1, MinorVersion = 1 },
                SequenceNumber = (ushort)sequenceNumber,
                Status = StatusCodes.Bad,
                Timestamp = DateTimeOffset.UtcNow,
                UseCompatibilityMode = useCompatibilityMode,
                MessageType = MessageType.KeyFrame,
                Picoseconds = 1,
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
            return new DataSet(new Dictionary<string, DataValue> {
                { "1", new DataValue(new Variant(true), StatusCodes.Good, DateTime.Now, DateTime.UtcNow) },
                { "2", new DataValue(new Variant(0.5), StatusCodes.Good, DateTime.Now) },
                { "3", new DataValue("abcd") }
            }, dataSetFieldContentMask);
        }
    }
}
