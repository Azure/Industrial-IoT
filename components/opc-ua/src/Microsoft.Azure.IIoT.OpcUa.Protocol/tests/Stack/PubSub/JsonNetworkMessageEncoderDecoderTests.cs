﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.PubSub.Tests {
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Opc.Ua.Encoders;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    /// <summary>
    /// Json encoder decoder tests
    /// </summary>
    public class JsonNetworkMessageEncoderDecoderTests {

        public const JsonNetworkMessageContentMask NetworkMessageContentMaskDefault =
            JsonNetworkMessageContentMask.PublisherId |
            JsonNetworkMessageContentMask.NetworkMessageHeader |
            JsonNetworkMessageContentMask.DataSetMessageHeader |
            JsonNetworkMessageContentMask.DataSetClassId;

        public const JsonDataSetMessageContentMask DataSetMessageContentMaskDefault =
            JsonDataSetMessageContentMask2.DataSetWriterName |
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
            bool useCompatibilityMode, JsonNetworkMessageContentMask contentMask, int numberOfMessages) {
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
            bool useCompatibilityMode, JsonNetworkMessageContentMask contentMask, int numberOfMessages) {
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

            var result = PubSubMessage.Decode(buffer, networkMessage.ContentType, context);
            Assert.Equal(networkMessage, result);
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
            bool useCompatibilityMode, JsonNetworkMessageContentMask contentMask, int numberOfMessages, int maxMessageSize) {
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
            bool useCompatibilityMode, JsonNetworkMessageContentMask contentMask, int numberOfMessages, int maxMessageSize) {
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
        [InlineData(true, true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        [InlineData(false, false, NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        public void EncodeDecodeNetworkMessagesNoNetworkMessageHeader(bool useArrayEnvelope,
            bool useCompatibilityMode, JsonNetworkMessageContentMask contentMask, int numberOfMessages, int maxMessageSize) {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(useCompatibilityMode, sequenceNumber))
                .ToList();
            var networkMessage = CreateNetworkMessage(contentMask & ~JsonNetworkMessageContentMask.NetworkMessageHeader, messages);
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
        [InlineData(false, false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, false, NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        [InlineData(false, false, NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        public void EncodeDecodeNetworkMessagesNoDataSetMessageHeader(bool useArrayEnvelope,
            bool useCompatibilityMode, JsonNetworkMessageContentMask contentMask, int numberOfMessages, int maxMessageSize) {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(useCompatibilityMode, sequenceNumber))
                .ToList();
            var networkMessage = CreateNetworkMessage(contentMask
                & ~JsonNetworkMessageContentMask.DataSetMessageHeader, messages);
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
        [InlineData(false, false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, false, NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        [InlineData(false, false, NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        public void EncodeDecodeNetworkMessagesNoHeader(bool useArrayEnvelope,
            bool useCompatibilityMode, JsonNetworkMessageContentMask contentMask, int numberOfMessages, int maxMessageSize) {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(useCompatibilityMode, sequenceNumber))
                .ToList();
            var networkMessage = CreateNetworkMessage(contentMask
                & ~(JsonNetworkMessageContentMask.NetworkMessageHeader | JsonNetworkMessageContentMask.DataSetMessageHeader), messages);
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
        [InlineData(false, false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, false, NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, false, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        [InlineData(false, false, NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(false, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(true, true, NetworkMessageContentMaskDefault | JsonNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        public void EncodeDecodeNetworkMessagesNoHeaderRaw(bool useArrayEnvelope,
            bool useCompatibilityMode, JsonNetworkMessageContentMask contentMask, int numberOfMessages, int maxMessageSize) {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(useCompatibilityMode, sequenceNumber,
                    dataSetFieldContentMask: DataSetFieldContentMask.RawData))
                .ToList();
            var networkMessage = CreateNetworkMessage(contentMask
                & ~(JsonNetworkMessageContentMask.NetworkMessageHeader | JsonNetworkMessageContentMask.DataSetMessageHeader), messages);
            networkMessage.UseArrayEnvelope = useArrayEnvelope;

            var context = new ServiceMessageContext();
            var buffers = networkMessage.Encode(context, maxMessageSize);

            ConvertToOpcUaUniversalTime(networkMessage);

            // Compare payload as raw data equivalent
            var serializer = new NewtonSoftJsonSerializer();
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
        private static void ConvertToOpcUaUniversalTime(BaseNetworkMessage networkMessage) {
            // convert DataSet Payload DataValue timestamps to OpcUa Utc
            foreach (var dataSetMessage in networkMessage.Messages) {
                var expectedPayload = new Dictionary<string, DataValue>();
                foreach (var entry in dataSetMessage.Payload) {
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
        private static JsonNetworkMessage CreateNetworkMessage(
            JsonNetworkMessageContentMask contentMask, List<BaseDataSetMessage> messages) {
            var networkMessage = new JsonNetworkMessage {
                MessageId = Guid.NewGuid().ToString(), // TODO
                Messages = messages,
                DataSetWriterGroup = "group",
                DataSetClassId = Guid.NewGuid(),
                PublisherId = "PublisherId",
                NetworkMessageContentMask = (uint)contentMask
            };
            return networkMessage;
        }
        /// <summary>
        /// Create dataset message
        /// </summary>
        private static JsonDataSetMessage CreateDataSetMessage(bool useCompatibilityMode, int sequenceNumber,
            JsonDataSetMessageContentMask dataSetMessageContentMask = DataSetMessageContentMaskDefault,
            DataSetFieldContentMask dataSetFieldContentMask = DataSetFieldContentMaskDefault) {
            return new JsonDataSetMessage {
                DataSetWriterName = "WriterId",
                DataSetWriterId = (ushort)(useCompatibilityMode ? 0 : 3),
                MetaDataVersion = new ConfigurationVersionDataType { MajorVersion = 1, MinorVersion = 1 },
                SequenceNumber = (ushort)sequenceNumber,
                Status = StatusCodes.Bad,
                Timestamp = DateTime.UtcNow,
                UseCompatibilityMode = useCompatibilityMode,
                MessageType = MessageType.KeyFrame,
                Picoseconds = 1,
                Payload = CreateDataSet(dataSetFieldContentMask),
                DataSetMessageContentMask = (uint)dataSetMessageContentMask
            };
        }

        /// <summary>
        /// Create dataset
        /// </summary>
        private static DataSet CreateDataSet(DataSetFieldContentMask dataSetFieldContentMask = DataSetFieldContentMaskDefault) {
            return new DataSet(new Dictionary<string, DataValue> {
                { "1", new DataValue(new Variant(5), StatusCodes.Good, DateTime.Now, DateTime.UtcNow) },
                { "2", new DataValue(new Variant(0.5), StatusCodes.Good, DateTime.Now) },
                { "3", new DataValue("abcd") }
            }, (uint)dataSetFieldContentMask);
        }
    }
}
