﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub
{
    using Azure.IIoT.OpcUa.Encoders.Models;
    using Azure.IIoT.OpcUa.Encoders.Schemas;
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
    public class AvroNetworkMessageEncoderDecoderTests
    {
        public const JsonNetworkMessageContentMask NetworkMessageContentMaskDefault =
            JsonNetworkMessageContentMask.PublisherId |
            JsonNetworkMessageContentMask.NetworkMessageHeader |
            JsonNetworkMessageContentMask.DataSetMessageHeader |
            JsonNetworkMessageContentMask.DataSetClassId;

        public const JsonDataSetMessageContentMask DataSetMessageContentMaskDefault =
            JsonDataSetMessageContentMask.DataSetWriterName |
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
        [InlineData(false, false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 1)]
        [InlineData(false, false, NetworkMessageContentMaskDefault, 3)]
        [InlineData(false, false, NetworkMessageContentMaskDefault, 1)]
        [InlineData(false, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 1)]
        [InlineData(false, true, NetworkMessageContentMaskDefault, 3)]
        [InlineData(true, false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 1)]
        [InlineData(true, false, NetworkMessageContentMaskDefault, 3)]
        [InlineData(true, false, NetworkMessageContentMaskDefault, 1)]
        [InlineData(true, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 1)]
        [InlineData(true, true, NetworkMessageContentMaskDefault, 3)]
        public void EncodeDecodeNetworkMessage(bool compress,
            bool useCompatibilityMode, JsonNetworkMessageContentMask contentMask, int numberOfMessages)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(useCompatibilityMode, sequenceNumber))
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
        [InlineData(false, false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 1)]
        [InlineData(false, false, NetworkMessageContentMaskDefault, 3)]
        [InlineData(false, false, NetworkMessageContentMaskDefault, 1)]
        [InlineData(false, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 1)]
        [InlineData(false, true, NetworkMessageContentMaskDefault, 3)]
        [InlineData(true, false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 1)]
        [InlineData(true, false, NetworkMessageContentMaskDefault, 3)]
        [InlineData(true, false, NetworkMessageContentMaskDefault, 1)]
        [InlineData(true, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 1)]
        [InlineData(true, true, NetworkMessageContentMaskDefault, 3)]
        public void EncodeDecodeNetworkMessageReversible(bool compress,
            bool useCompatibilityMode, JsonNetworkMessageContentMask contentMask, int numberOfMessages)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(useCompatibilityMode, sequenceNumber,
                    DataSetMessageContentMaskDefault | JsonDataSetMessageContentMask.ReversibleFieldEncoding))
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
        [InlineData(false, false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, false, NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        [InlineData(false, false, NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        [InlineData(true, false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(true, false, NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(true, false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        [InlineData(true, false, NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        public void EncodeDecodeNetworkMessages(bool compress,
            bool useCompatibilityMode, JsonNetworkMessageContentMask contentMask, int numberOfMessages, int maxMessageSize)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(useCompatibilityMode, sequenceNumber))
                .ToList();
            var networkMessage = CreateNetworkMessage(contentMask, messages);
            networkMessage.UseGzipCompression = compress;

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
        [InlineData(false, false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, false, NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        [InlineData(false, false, NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        [InlineData(true, false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(true, false, NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(true, false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        [InlineData(true, false, NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        public void EncodeDecodeNetworkMessagesReversible(bool compress,
            bool useCompatibilityMode, JsonNetworkMessageContentMask contentMask, int numberOfMessages, int maxMessageSize)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(useCompatibilityMode, sequenceNumber,
                    DataSetMessageContentMaskDefault | JsonDataSetMessageContentMask.ReversibleFieldEncoding))
                .ToList();
            var networkMessage = CreateNetworkMessage(contentMask, messages);
            networkMessage.UseGzipCompression = compress;

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
        [InlineData(false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        [InlineData(false, NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        public void EncodeDecodeNetworkMessagesNoNetworkMessageHeader(
            bool useCompatibilityMode, JsonNetworkMessageContentMask contentMask, int numberOfMessages, int maxMessageSize)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(useCompatibilityMode, sequenceNumber))
                .ToList();
            var networkMessage = CreateNetworkMessage(contentMask & ~JsonNetworkMessageContentMask.NetworkMessageHeader, messages);

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
        [InlineData(false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        [InlineData(false, NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        public void EncodeDecodeNetworkMessagesNoDataSetMessageHeader(
            bool useCompatibilityMode, JsonNetworkMessageContentMask contentMask, int numberOfMessages, int maxMessageSize)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(useCompatibilityMode, sequenceNumber))
                .ToList();
            var networkMessage = CreateNetworkMessage(contentMask
                & ~JsonNetworkMessageContentMask.DataSetMessageHeader, messages);

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
        [InlineData(false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        [InlineData(false, NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        public void EncodeDecodeNetworkMessagesNoHeader(
            bool useCompatibilityMode, JsonNetworkMessageContentMask contentMask, int numberOfMessages, int maxMessageSize)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(useCompatibilityMode, sequenceNumber))
                .ToList();
            var networkMessage = CreateNetworkMessage(contentMask
                & ~(JsonNetworkMessageContentMask.NetworkMessageHeader | JsonNetworkMessageContentMask.DataSetMessageHeader), messages);

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
        [InlineData(false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        [InlineData(false, NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        public void EncodeDecodeNetworkMessagesNoHeaderRaw(
            bool useCompatibilityMode, JsonNetworkMessageContentMask contentMask, int numberOfMessages, int maxMessageSize)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(useCompatibilityMode, sequenceNumber,
                    dataSetFieldContentMask: DataSetFieldContentMask.RawData))
                .ToList();
            var networkMessage = CreateNetworkMessage(contentMask
                & ~(JsonNetworkMessageContentMask.NetworkMessageHeader | JsonNetworkMessageContentMask.DataSetMessageHeader), messages);

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
        private static AvroNetworkMessage CreateNetworkMessage(
            JsonNetworkMessageContentMask contentMask, List<BaseDataSetMessage> messages)
        {
            return new AvroNetworkMessage(null)
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
        private static AvroDataSetMessage CreateDataSetMessage(bool useCompatibilityMode, int sequenceNumber,
            JsonDataSetMessageContentMask dataSetMessageContentMask = DataSetMessageContentMaskDefault,
            DataSetFieldContentMask dataSetFieldContentMask = DataSetFieldContentMaskDefault)
        {
            return new AvroDataSetMessage
            {
                DataSetWriterName = "WriterId",
                DataSetWriterId = (ushort)(useCompatibilityMode ? 0 : 3),
                MetaDataVersion = new ConfigurationVersionDataType { MajorVersion = 1, MinorVersion = 1 },
                SequenceNumber = (ushort)sequenceNumber,
                Status = StatusCodes.Bad,
                Timestamp = DateTime.UtcNow,
                MessageType = MessageType.KeyFrame,
                Picoseconds = 1,
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
                { "1", new DataValue(new Variant(true), StatusCodes.Good, DateTime.Now, DateTime.UtcNow) },
                { "2", new DataValue(new Variant(0.5), StatusCodes.Good, DateTime.Now) },
                { "3", new DataValue("abcd") }
            }, (uint)dataSetFieldContentMask);
        }
    }
}
