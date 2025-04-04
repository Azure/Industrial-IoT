﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub
{
    using Azure.IIoT.OpcUa.Encoders.Models;
    using Azure.IIoT.OpcUa.Encoders.Schemas.Avro;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    /// <summary>
    /// Avro encoder decoder tests
    /// </summary>
    public class AvroNetworkMessageEncoderDecoderTests2
    {
        public const NetworkMessageContentFlags NetworkMessageContentMaskDefault =
            NetworkMessageContentFlags.NetworkMessageHeader |
            NetworkMessageContentFlags.DataSetMessageHeader;

        public const DataSetFieldContentFlags DataSetFieldContentFlagsDefault =
            DataSetFieldContentFlags.SourceTimestamp |
            DataSetFieldContentFlags.ServerTimestamp |
            DataSetFieldContentFlags.SourcePicoSeconds |
            DataSetFieldContentFlags.ServerPicoSeconds |
            DataSetFieldContentFlags.StatusCode;

        [Theory]
        [InlineData(false, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 1)]
        [InlineData(false, NetworkMessageContentMaskDefault, 3)]
        [InlineData(false, NetworkMessageContentMaskDefault, 1)]
        [InlineData(true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 1)]
        [InlineData(true, NetworkMessageContentMaskDefault, 3)]
        [InlineData(true, NetworkMessageContentMaskDefault, 1)]
        public void EncodeDecodeNetworkMessage(bool compress,
            NetworkMessageContentFlags contentMask, int numberOfMessages)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(sequenceNumber))
                .ToList();
            var networkMessage = CreateNetworkMessage(contentMask, messages);
            networkMessage.UseGzipCompression = compress;

            var context = new ServiceMessageContext();
            var buffer = Assert.Single(networkMessage.Encode(context, 256 * 1000));
            var schema = networkMessage.Schema;
            Assert.NotNull(schema);
            var json = schema.ToJson();

            context = new ServiceMessageContext();
            buffer = Assert.Single(networkMessage.Encode(context, 256 * 1000));
            Assert.Equal(schema, networkMessage.Schema);

            ConvertToOpcUaUniversalTime(networkMessage);

            var result = PubSubMessage.Decode(buffer, networkMessage.ContentType, context, messageSchema: json);
            Assert.Equal(networkMessage, result);
        }

        [Theory]
        [InlineData(false, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(false, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(false, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 1024)]
        [InlineData(false, NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(false, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(true, NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 1024)]
        [InlineData(true, NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(true, NetworkMessageContentMaskDefault, 15, 1024)]
        public void EncodeDecodeNetworkMessages(bool compress,
            NetworkMessageContentFlags contentMask, int numberOfMessages, int maxMessageSize)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(sequenceNumber))
                .ToList();
            var networkMessage = CreateNetworkMessage(contentMask, messages);
            networkMessage.UseGzipCompression = compress;

            var context = new ServiceMessageContext();
            var buffers = networkMessage.Encode(context, maxMessageSize);
            var schema = networkMessage.Schema;
            Assert.NotNull(schema);
            var json = schema.ToJson();

            ConvertToOpcUaUniversalTime(networkMessage);

            var m = buffers
                .Select(buffer => (BaseNetworkMessage)PubSubMessage
                    .Decode(buffer, networkMessage.ContentType, context, messageSchema: json))
                .ToList();
            var result = m[0];
            result.Messages = m.SelectMany(m => m.Messages).ToList();
            Assert.Equal(networkMessage, result);
        }

        [Theory]
        [InlineData(NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 15, 1024)]
        public void EncodeDecodeNetworkMessagesNoNetworkMessageHeader(
            NetworkMessageContentFlags contentMask, int numberOfMessages, int maxMessageSize)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(sequenceNumber))
                .ToList();
            var networkMessage = CreateNetworkMessage(contentMask & ~NetworkMessageContentFlags.NetworkMessageHeader, messages);

            var context = new ServiceMessageContext();
            var buffers = networkMessage.Encode(context, maxMessageSize);

            var schema = networkMessage.Schema;
            Assert.NotNull(schema);
            var json = schema.ToJson();
            ConvertToOpcUaUniversalTime(networkMessage);

            var result = buffers
                .SelectMany(buffer => ((BaseNetworkMessage)PubSubMessage
                    .Decode(buffer, networkMessage.ContentType, context, messageSchema: json)).Messages)
                .ToList();
            Assert.Equal(networkMessage.Messages, result);
        }

        [Theory]
        [InlineData(NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 15, 1024)]
        public void EncodeDecodeNetworkMessagesNoDataSetMessageHeader(
            NetworkMessageContentFlags contentMask, int numberOfMessages, int maxMessageSize)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(sequenceNumber))
                .ToList();
            var networkMessage = CreateNetworkMessage(contentMask
                & ~NetworkMessageContentFlags.DataSetMessageHeader, messages);

            var context = new ServiceMessageContext();
            var buffers = networkMessage.Encode(context, maxMessageSize);

            var schema = networkMessage.Schema;
            Assert.NotNull(schema);
            var json = schema.ToJson();
            ConvertToOpcUaUniversalTime(networkMessage);

            var result = buffers
                .SelectMany(buffer => ((BaseNetworkMessage)PubSubMessage
                    .Decode(buffer, networkMessage.ContentType, context, messageSchema: json)).Messages)
                .Select(m => m.Payload)
                .ToList();
            Assert.All(networkMessage.Messages.Select(m => m.Payload), (p, i) => Assert.True(result[i].Equals(p)));
        }

        [Theory]
        [InlineData(NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 15, 1024)]
        public void EncodeDecodeNetworkMessagesNoHeader(
            NetworkMessageContentFlags contentMask, int numberOfMessages, int maxMessageSize)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(sequenceNumber))
                .ToList();
            var networkMessage = CreateNetworkMessage(contentMask
                & ~(NetworkMessageContentFlags.NetworkMessageHeader | NetworkMessageContentFlags.DataSetMessageHeader), messages);

            var context = new ServiceMessageContext();
            var buffers = networkMessage.Encode(context, maxMessageSize);

            var schema = networkMessage.Schema;
            Assert.NotNull(schema);
            var json = schema.ToJson();
            ConvertToOpcUaUniversalTime(networkMessage);

            var result = buffers
                .SelectMany(buffer => ((BaseNetworkMessage)PubSubMessage
                    .Decode(buffer, networkMessage.ContentType, context, messageSchema: json)).Messages)
                .Select(m => m.Payload)
                .ToList();
            Assert.All(networkMessage.Messages.Select(m => m.Payload), (p, i) => Assert.True(result[i].Equals(p)));
        }

        [Theory]
        [InlineData(NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 15, 1024)]
        public void EncodeDecodeNetworkMessagesNoHeaderRaw(
            NetworkMessageContentFlags contentMask, int numberOfMessages, int maxMessageSize)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(sequenceNumber,
                    dataSetFieldContentMask: DataSetFieldContentFlags.RawData))
                .ToList();
            var networkMessage = CreateNetworkMessage(contentMask
                & ~(NetworkMessageContentFlags.NetworkMessageHeader | NetworkMessageContentFlags.DataSetMessageHeader), messages);

            var context = new ServiceMessageContext();
            var buffers = networkMessage.Encode(context, maxMessageSize);

            var schema = networkMessage.Schema;
            Assert.NotNull(schema);
            var json = schema.ToJson();
            ConvertToOpcUaUniversalTime(networkMessage);

            // Compare payload as raw data equivalent
            var serializer = new NewtonsoftJsonSerializer();
            var result = serializer.Parse(serializer.SerializeToString(buffers
                .SelectMany(buffer => ((BaseNetworkMessage)PubSubMessage
                    .Decode(buffer, networkMessage.ContentType, context, messageSchema: json)).Messages)
                .SelectMany(m => m.Payload.DataSetFields)
                .Select(v => (v.Name, v.Value.Value))
                .ToList()));
            var expected = serializer.Parse(serializer.SerializeToString(messages
                .SelectMany(m => m.Payload.DataSetFields)
                .Select(v => (v.Name, v.Value.Value))
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
                foreach (var (Name, Value) in dataSetMessage.Payload.DataSetFields)
                {
                    expectedPayload[Name] = new DataValue(Value).ToOpcUaUniversalTime();
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
        private static AvroNetworkMessage CreateNetworkMessage(
            NetworkMessageContentFlags contentMask, List<BaseDataSetMessage> messages)
        {
            return new AvroNetworkMessage
            {
                MessageId = () => "9279C0B3-DA88-45A4-AF74-451CEBF82DB0",
                Messages = messages,
                DataSetWriterGroup = "group",
                DataSetClassId = Guid.NewGuid(),
                PublisherId = "PublisherId",
                EmitConciseSchema = true,
                NetworkMessageContentMask = contentMask
            };
        }

        /// <summary>
        /// Create dataset message
        /// </summary>
        /// <param name="sequenceNumber"></param>
        /// <param name="dataSetFieldContentMask"></param>
        private static AvroDataSetMessage CreateDataSetMessage(int sequenceNumber,
            DataSetFieldContentFlags dataSetFieldContentMask = DataSetFieldContentFlagsDefault)
        {
            return new AvroDataSetMessage
            {
                DataSetWriterName = "WriterId",
                DataSetWriterId = 3,
                MetaDataVersion = new ConfigurationVersionDataType { MajorVersion = 1, MinorVersion = 1 },
                SequenceNumber = (ushort)sequenceNumber,
                Status = StatusCodes.Bad,
                Timestamp = DateTimeOffset.UtcNow,
                MessageType = MessageType.KeyFrame,
                Picoseconds = 1,
                Payload = CreateDataSet(dataSetFieldContentMask)
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
