// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub
{
    using Azure.IIoT.OpcUa.Encoders.Models;
    using Azure.IIoT.OpcUa.Encoders.Schemas.Avro;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Avro;
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
    public class AvroNetworkMessageEncoderDecoderTests1
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
        [InlineData(false, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 1, MessageType.KeyFrame)]
        [InlineData(false, NetworkMessageContentMaskDefault, 3, MessageType.Condition)]
        [InlineData(false, NetworkMessageContentMaskDefault, 1, MessageType.DeltaFrame)]
        [InlineData(true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 1, MessageType.KeyFrame)]
        [InlineData(true, NetworkMessageContentMaskDefault, 3, MessageType.KeepAlive)]
        [InlineData(true, NetworkMessageContentMaskDefault, 1, MessageType.Event)]
        public void EncodeDecodeNetworkMessage(bool compress,
            NetworkMessageContentFlags contentMask, int numberOfMessages, MessageType messageType)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(sequenceNumber,
                    messageType: messageType, statusCode: StatusCodes.Good))
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
        [InlineData(false, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 1, MessageType.KeyFrame)]
        [InlineData(false, NetworkMessageContentMaskDefault, 3, MessageType.Condition)]
        [InlineData(false, NetworkMessageContentMaskDefault, 1, MessageType.DeltaFrame)]
        [InlineData(true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 1, MessageType.KeyFrame)]
        [InlineData(true, NetworkMessageContentMaskDefault, 3, MessageType.KeepAlive)]
        [InlineData(true, NetworkMessageContentMaskDefault, 1, MessageType.Event)]
        public void EncodeDecodeNetworkMessageWithNullableDataValue(bool compress,
            NetworkMessageContentFlags contentMask, int numberOfMessages, MessageType messageType)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(sequenceNumber,
                    messageType: messageType, statusCode: null))
                .ToList();
            var networkMessage = CreateNetworkMessage(contentMask, messages);
            networkMessage.UseGzipCompression = compress;

            var context = new ServiceMessageContext();
            var buffer = Assert.Single(networkMessage.Encode(context, 256 * 1000));
            var schema = networkMessage.Schema;
            Assert.NotNull(schema);
            var json = schema.ToJson();

            // Set null data value
            messages.ForEach(messages => messages.Payload = messages.Payload.Set("6", null));
            // Reencode with the schema
            context = new ServiceMessageContext();
            buffer = Assert.Single(networkMessage.Encode(context, 256 * 1000));
            Assert.Equal(schema, networkMessage.Schema);

            ConvertToOpcUaUniversalTime(networkMessage);

            var result = PubSubMessage.Decode(buffer, networkMessage.ContentType, context, messageSchema: json);
            Assert.Equal(networkMessage, result);
        }

        [Theory]
        [InlineData(false, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 1, MessageType.KeyFrame)]
        [InlineData(false, NetworkMessageContentMaskDefault, 3, MessageType.Condition)]
        [InlineData(false, NetworkMessageContentMaskDefault, 1, MessageType.DeltaFrame)]
        [InlineData(true, NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 1, MessageType.KeyFrame)]
        [InlineData(true, NetworkMessageContentMaskDefault, 3, MessageType.KeepAlive)]
        [InlineData(true, NetworkMessageContentMaskDefault, 1, MessageType.Event)]
        public void EncodeDecodeNetworkMessageWithMissingDataValue(bool compress,
            NetworkMessageContentFlags contentMask, int numberOfMessages, MessageType messageType)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(sequenceNumber,
                    messageType: messageType, statusCode: null))
                .ToList();
            var networkMessage = CreateNetworkMessage(contentMask, messages);
            networkMessage.UseGzipCompression = compress;

            var context = new ServiceMessageContext();
            var buffer = Assert.Single(networkMessage.Encode(context, 256 * 1000));
            var schema = networkMessage.Schema;
            Assert.NotNull(schema);
            var json = schema.ToJson();

            // Set null data value
            messages.ForEach(messages => messages.Payload = messages.Payload.Remove("6"));
            // Reencode with the schema
            context = new ServiceMessageContext();
            buffer = Assert.Single(networkMessage.Encode(context, 256 * 1000));
            Assert.Equal(schema, networkMessage.Schema);

            ConvertToOpcUaUniversalTime(networkMessage);

            var result = PubSubMessage.Decode(buffer, networkMessage.ContentType, context, messageSchema: json);
            // Result will contain the removed field in the data set as it was serialized as null
            ((BaseNetworkMessage)result).Messages.ToList().ForEach(m =>
            {
                Assert.Null(m.Payload.DataSetFields.FirstOrDefault(f => f.Name == "6").Value);
                m.Payload = m.Payload.Remove("6");
            });
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
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(sequenceNumber, messageType: MessageType.DeltaFrame))
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
        public void EncodeDecodeNetworkMessagesWithNullableDataValue(bool compress,
            NetworkMessageContentFlags contentMask, int numberOfMessages, int maxMessageSize)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(sequenceNumber, messageType: MessageType.DeltaFrame))
                .ToList();
            var networkMessage = CreateNetworkMessage(contentMask, messages);
            networkMessage.UseGzipCompression = compress;

            var context = new ServiceMessageContext();
            var buffers = networkMessage.Encode(context, maxMessageSize);
            var schema = networkMessage.Schema;
            Assert.NotNull(schema);
            var json = schema.ToJson();

            // Set null data value
            messages.ForEach(messages => messages.Payload = messages.Payload.Set("6", null));
            // Reencode with the schema
            context = new ServiceMessageContext();
            buffers = networkMessage.Encode(context, maxMessageSize);
            Assert.Equal(schema, networkMessage.Schema);

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
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(sequenceNumber, messageType: MessageType.Event))
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
        public void EncodeDecodeNetworkMessagesNoNetworkMessageHeaderRaw(
            NetworkMessageContentFlags contentMask, int numberOfMessages, int maxMessageSize)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(sequenceNumber,
                    dataSetFieldContentMask: DataSetFieldContentFlags.RawData, messageType: MessageType.Condition))
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
                .SelectMany(m => m.Payload.DataSetFields)
                .Select(v => (v.Name, v.Value?.Value))
                .ToList();
            var serializer = new NewtonsoftJsonSerializer();
            var expected = serializer.Parse(serializer.SerializeToString(messages
                .SelectMany(m => m.Payload.DataSetFields)
                .Select(v => (v.Name, v.Value?.Value))
                .ToList()));
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
        [InlineData(NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 15, 1024)]
        public void EncodeDecodeNetworkMessagesNoDataSetMessageHeaderWithNullableDataValues(
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

            // Set null data value
            messages.ForEach(messages => messages.Payload = messages.Payload.Set("6", null));
            // Reencode with the schema
            context = new ServiceMessageContext();
            buffers = networkMessage.Encode(context, maxMessageSize);
            Assert.Equal(schema, networkMessage.Schema);
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
        public void EncodeDecodeNetworkMessagesNoDataSetMessageHeaderRaw(
            NetworkMessageContentFlags contentMask, int numberOfMessages, int maxMessageSize)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(sequenceNumber,
                    dataSetFieldContentMask: DataSetFieldContentFlags.RawData))
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
                .SelectMany(m => m.Payload.DataSetFields)
                .Select(v => (v.Name, v.Value?.Value))
                .ToList();
            var serializer = new NewtonsoftJsonSerializer();

            var expected = serializer.Parse(serializer.SerializeToString(messages
                .SelectMany(m => m.Payload.DataSetFields)
                .Select(v => (v.Name, v.Value?.Value))
                .ToList()));
        }

        [Theory]
        [InlineData(NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 15, 256 * 1024)]
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
        [InlineData(5, 256 * 1024)]
        [InlineData(10, 256 * 1024)]
        [InlineData(15, 256 * 1024)]
        [InlineData(5, 1024)]
        [InlineData(10, 1024)]
        [InlineData(15, 1024)]
        public void EncodeDecodeSingleMessageWithUnionSchemas(int numberOfMessages, int maxMessageSize)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(sequenceNumber))
                .ToList();
            var networkMessage = CreateNetworkMessage(NetworkMessageContentFlags.SingleDataSetMessage, messages);

            var context = new ServiceMessageContext();
            var buffers = networkMessage.Encode(context, maxMessageSize);

            var schema = networkMessage.Schema;
            var json = schema.ToJson();
            var union = UnionSchema.Create(Enumerable.Range(0, 4)
                .Select(i => Schema.Parse(schema.ToJson().Replace("\"DataSet\"", $"\"DataSet{i}\"", StringComparison.InvariantCulture)))
                .ToList());
            json = union.ToJson();

            // Re-encode
            networkMessage.Schema = union;
            buffers = networkMessage.Encode(context, maxMessageSize);

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
                .Select(v => (v.Name, v.Value?.Value))
                .ToList()));
            var expected = serializer.Parse(serializer.SerializeToString(messages
                .SelectMany(m => m.Payload.DataSetFields)
                .Select(v => (v.Name, v.Value?.Value))
                .ToList()));

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 15, 1024)]
        public void EncodeDecodeNetworkMessagesNoHeaderRawAndNullableDataValue(
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

            // Set null data value
            messages.ForEach(messages => messages.Payload = messages.Payload.Set("6", null));
            // Reencode with the schema
            context = new ServiceMessageContext();
            buffers = networkMessage.Encode(context, maxMessageSize);
            Assert.Equal(schema, networkMessage.Schema);

            ConvertToOpcUaUniversalTime(networkMessage);

            // Compare payload as raw data equivalent
            var serializer = new NewtonsoftJsonSerializer();
            var result = serializer.Parse(serializer.SerializeToString(buffers
                .SelectMany(buffer => ((BaseNetworkMessage)PubSubMessage
                    .Decode(buffer, networkMessage.ContentType, context, messageSchema: json)).Messages)
                .SelectMany(m => m.Payload.DataSetFields)
                .Select(v => (v.Name, v.Value?.Value))
                .ToList()));
            var expected = serializer.Parse(serializer.SerializeToString(messages
                .SelectMany(m => m.Payload.DataSetFields)
                .Select(v => (v.Name, v.Value?.Value))
                .ToList()));

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(NetworkMessageContentMaskDefault | NetworkMessageContentFlags.SingleDataSetMessage, 5, 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 15, 1024)]
        public void EncodeDecodeNetworkMessagesNoHeaderRawAndMissingDataValue(
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

            // Set null data value
            messages.ForEach(messages => messages.Payload = messages.Payload.Remove("6"));
            // Reencode with the schema
            context = new ServiceMessageContext();
            buffers = networkMessage.Encode(context, maxMessageSize);
            Assert.Equal(schema, networkMessage.Schema);

            ConvertToOpcUaUniversalTime(networkMessage);

            // Compare payload as raw data equivalent
            var serializer = new NewtonsoftJsonSerializer();
            var result = serializer.Parse(serializer.SerializeToString(buffers
                .SelectMany(buffer => ((BaseNetworkMessage)PubSubMessage
                    .Decode(buffer, networkMessage.ContentType, context, messageSchema: json)).Messages)
                .SelectMany(m => m.Payload.DataSetFields)
                .Where(m => m.Name != "6")
                .Select(v => (v.Name, v.Value?.Value))
                .ToList()));
            var expected = serializer.Parse(serializer.SerializeToString(messages
                .SelectMany(m => m.Payload.DataSetFields)
                .Select(v => (v.Name, v.Value?.Value))
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
                    expectedPayload[Name] = Value == null ? null : new DataValue(Value).ToOpcUaUniversalTime();
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
                NetworkMessageContentMask = contentMask
            };
        }

        /// <summary>
        /// Create dataset message
        /// </summary>
        /// <param name="sequenceNumber"></param>
        /// <param name="dataSetFieldContentMask"></param>
        /// <param name="messageType"></param>
        /// <param name="statusCode"></param>
        private static AvroDataSetMessage CreateDataSetMessage(int sequenceNumber,
            DataSetFieldContentFlags dataSetFieldContentMask = DataSetFieldContentFlagsDefault,
            MessageType messageType = MessageType.KeyFrame, uint? statusCode = StatusCodes.Bad)
        {
            return new AvroDataSetMessage
            {
                DataSetWriterName = "WriterId",
                DataSetWriterId = 3,
                MetaDataVersion = new ConfigurationVersionDataType { MajorVersion = 1, MinorVersion = 1 },
                SequenceNumber = (ushort)sequenceNumber,
                Status = statusCode,
                Timestamp = DateTimeOffset.UtcNow,
                MessageType = messageType,
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
                { "3", new DataValue(Variant.Null, StatusCodes.Good, DateTime.Now) },
                { "4", new DataValue(new Variant(new string[]{"test", "Test"}), StatusCodes.Good, DateTime.Now) },
                { "5", new DataValue(new Variant(Array.Empty<float>()), StatusCodes.Good, DateTime.Now) },
                { "6", new DataValue() },
                { "7", new DataValue("abcd") }
            }, dataSetFieldContentMask);
        }
    }
}
