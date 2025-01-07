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
    using System.Buffers;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    /// <summary>
    /// Uadp encoder decoder tests
    /// </summary>
    public class UadpNetworkMessageEncoderDecoderTests : IDataSetMetaDataResolver
    {
        /// <summary>
        /// All
        /// </summary>
        public const NetworkMessageContentFlags NetworkMessageContentMaskDefault =
            NetworkMessageContentFlags.PublisherId |
            NetworkMessageContentFlags.GroupHeader |
            NetworkMessageContentFlags.WriterGroupId |
            NetworkMessageContentFlags.GroupVersion |
            NetworkMessageContentFlags.NetworkMessageNumber |
            NetworkMessageContentFlags.SequenceNumber |
            NetworkMessageContentFlags.PayloadHeader |
            NetworkMessageContentFlags.Timestamp |
            NetworkMessageContentFlags.Picoseconds |
            NetworkMessageContentFlags.DataSetClassId |
            NetworkMessageContentFlags.PromotedFields;

        public const DataSetMessageContentFlags DataSetMessageContentMaskDefault =
            DataSetMessageContentFlags.PicoSeconds |
            DataSetMessageContentFlags.SequenceNumber |
            DataSetMessageContentFlags.MajorVersion |
            DataSetMessageContentFlags.MinorVersion |
            DataSetMessageContentFlags.Timestamp |
            DataSetMessageContentFlags.Status;

        public const DataSetFieldContentFlags DataSetFieldContentFlagsDefault =
            DataSetFieldContentFlags.SourceTimestamp |
            DataSetFieldContentFlags.ServerTimestamp |
            DataSetFieldContentFlags.SourcePicoSeconds |
            DataSetFieldContentFlags.ServerPicoSeconds |
            DataSetFieldContentFlags.StatusCode;

        [Theory]
        [InlineData(MessageType.KeyFrame, 1)]
        [InlineData(MessageType.KeyFrame, 3)]
        [InlineData(MessageType.KeyFrame, 10)]
        [InlineData(MessageType.Event, 1)]
        [InlineData(MessageType.Event, 3)]
        [InlineData(MessageType.Event, 10)]
        [InlineData(MessageType.DeltaFrame, 1)]
        [InlineData(MessageType.DeltaFrame, 3)]
        [InlineData(MessageType.DeltaFrame, 10)]
        public void EncodeDecodeNetworkMessage(MessageType type, int numberOfMessages)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(type, sequenceNumber))
                .ToList();
            var networkMessage = CreateNetworkMessage(NetworkMessageContentMaskDefault, messages);

            var context = new ServiceMessageContext();
            var buffer = Assert.Single(networkMessage.Encode(context, 256 * 1000, this));

            ConvertToOpcUaUniversalTime(networkMessage);

            var result = PubSubMessage.Decode(buffer, networkMessage.ContentType, context, this);
            Assert.Equal(networkMessage, result);
        }

        [Theory]
        [InlineData(MessageType.KeyFrame, 8, 100)]
        [InlineData(MessageType.KeyFrame, 10, 100)]
        [InlineData(MessageType.KeyFrame, 100, 100)]
        [InlineData(MessageType.KeyFrame, 1000, 100)]
        [InlineData(MessageType.KeyFrame, 194, 100)]
        [InlineData(MessageType.KeyFrame, 8, 1024)]
        [InlineData(MessageType.KeyFrame, 10, 1024)]
        [InlineData(MessageType.KeyFrame, 100, 1024)]
        [InlineData(MessageType.KeyFrame, 1000, 1024)]
        [InlineData(MessageType.KeyFrame, 194, 1024)]
        [InlineData(MessageType.KeyFrame, 8, 256 * 1024)]
        [InlineData(MessageType.KeyFrame, 10, 256 * 1024)]
        [InlineData(MessageType.KeyFrame, 100, 256 * 1024)]
        [InlineData(MessageType.KeyFrame, 1000, 256 * 1024)]
        [InlineData(MessageType.KeyFrame, 194, 256 * 1024)]
        [InlineData(MessageType.Event, 1, 100)]
        [InlineData(MessageType.Event, 5, 100)]
        [InlineData(MessageType.Event, 194, 100)]
        [InlineData(MessageType.Event, 1, 1024)]
        [InlineData(MessageType.Event, 5, 1024)]
        [InlineData(MessageType.Event, 194, 1024)]
        [InlineData(MessageType.Event, 1, 256 * 1024)]
        [InlineData(MessageType.Event, 5, 256 * 1024)]
        [InlineData(MessageType.Event, 194, 256 * 1024)]
        [InlineData(MessageType.DeltaFrame, 1, 100)]
        [InlineData(MessageType.DeltaFrame, 5, 100)]
        [InlineData(MessageType.DeltaFrame, 194, 100)]
        [InlineData(MessageType.DeltaFrame, 1, 1024)]
        [InlineData(MessageType.DeltaFrame, 5, 1024)]
        [InlineData(MessageType.DeltaFrame, 194, 1024)]
        [InlineData(MessageType.DeltaFrame, 1, 256 * 1024)]
        [InlineData(MessageType.DeltaFrame, 5, 256 * 1024)]
        [InlineData(MessageType.DeltaFrame, 194, 256 * 1024)]
        public void EncodeDecodeNetworkMessages(MessageType type, int numberOfMessages, int maxMessageSize)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(type, sequenceNumber))
                .ToList();
            var networkMessage = CreateNetworkMessage(NetworkMessageContentMaskDefault, messages);

            var context = new ServiceMessageContext();
            var buffers = networkMessage.Encode(context, maxMessageSize, this);

            ConvertToOpcUaUniversalTime(networkMessage);

            var decodedMessages = PubSubMessage
                .Decode(CreateReader(buffers), networkMessage.ContentType, context, this)
                .OfType<UadpNetworkMessage>()
                .ToList();
            var result = decodedMessages[0];
            result.Messages = decodedMessages.SelectMany(m => m.Messages).ToList();
            Assert.Equal(networkMessage, result);
        }

        [Theory]
        [InlineData(MessageType.KeyFrame, 1, 100)]
        [InlineData(MessageType.KeyFrame, 5, 100)]
        [InlineData(MessageType.KeyFrame, 194, 100)]
        [InlineData(MessageType.KeyFrame, 1, 1024)]
        [InlineData(MessageType.KeyFrame, 5, 1024)]
        [InlineData(MessageType.KeyFrame, 194, 1024)]
        [InlineData(MessageType.KeyFrame, 1, 256 * 1024)]
        [InlineData(MessageType.KeyFrame, 5, 256 * 1024)]
        [InlineData(MessageType.KeyFrame, 194, 256 * 1024)]
        [InlineData(MessageType.DeltaFrame, 1, 100)]
        [InlineData(MessageType.DeltaFrame, 5, 100)]
        [InlineData(MessageType.DeltaFrame, 194, 100)]
        [InlineData(MessageType.DeltaFrame, 1, 1024)]
        [InlineData(MessageType.DeltaFrame, 5, 1024)]
        [InlineData(MessageType.DeltaFrame, 194, 1024)]
        [InlineData(MessageType.DeltaFrame, 1, 256 * 1024)]
        [InlineData(MessageType.DeltaFrame, 5, 256 * 1024)]
        [InlineData(MessageType.DeltaFrame, 194, 256 * 1024)]
        [InlineData(MessageType.Event, 1, 100)]
        [InlineData(MessageType.Event, 194, 100)]
        [InlineData(MessageType.Event, 1, 1024)]
        [InlineData(MessageType.Event, 194, 1024)]
        [InlineData(MessageType.Event, 194, 256 * 1024)]
        public void EncodeDecodeNetworkMessagesNoGroupHeader(MessageType type, int numberOfMessages, int maxMessageSize)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(type, sequenceNumber))
                .ToList();
            var networkMessage = CreateNetworkMessage(
                NetworkMessageContentMaskDefault & ~NetworkMessageContentFlags.GroupHeader, messages);

            var context = new ServiceMessageContext();
            var buffers = networkMessage.Encode(context, maxMessageSize, this);

            ConvertToOpcUaUniversalTime(networkMessage);

            var m = PubSubMessage
                .Decode(CreateReader(buffers), networkMessage.ContentType, context, this)
                .OfType<UadpNetworkMessage>()
                .ToList();
            var result = m[0];
            result.Messages = m.SelectMany(m => m.Messages).ToList();
            Assert.Equal(networkMessage, result);
        }

        [Theory]
        [InlineData(MessageType.KeyFrame, 1, 100)]
        [InlineData(MessageType.KeyFrame, 5, 100)]
        [InlineData(MessageType.KeyFrame, 194, 100)]
        [InlineData(MessageType.KeyFrame, 1, 1024)]
        [InlineData(MessageType.KeyFrame, 5, 1024)]
        [InlineData(MessageType.KeyFrame, 194, 1024)]
        [InlineData(MessageType.KeyFrame, 1, 256 * 1024)]
        [InlineData(MessageType.KeyFrame, 5, 256 * 1024)]
        [InlineData(MessageType.KeyFrame, 194, 256 * 1024)]
        [InlineData(MessageType.DeltaFrame, 1, 100)]
        [InlineData(MessageType.DeltaFrame, 5, 100)]
        [InlineData(MessageType.DeltaFrame, 194, 100)]
        [InlineData(MessageType.DeltaFrame, 1, 1024)]
        [InlineData(MessageType.DeltaFrame, 5, 1024)]
        [InlineData(MessageType.DeltaFrame, 194, 1024)]
        [InlineData(MessageType.DeltaFrame, 1, 256 * 1024)]
        [InlineData(MessageType.DeltaFrame, 5, 256 * 1024)]
        [InlineData(MessageType.DeltaFrame, 194, 256 * 1024)]
        [InlineData(MessageType.Event, 1, 100)]
        [InlineData(MessageType.Event, 194, 100)]
        [InlineData(MessageType.Event, 1, 1024)]
        [InlineData(MessageType.Event, 194, 1024)]
        [InlineData(MessageType.Event, 194, 256 * 1024)]
        public void EncodeDecodeNetworkMessagesNoPayloadHeader(MessageType type, int numberOfMessages, int maxMessageSize)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(type, sequenceNumber))
                .ToList();
            var networkMessage = CreateNetworkMessage(
                NetworkMessageContentMaskDefault & ~NetworkMessageContentFlags.PayloadHeader, messages);

            var context = new ServiceMessageContext();
            var buffers = networkMessage.Encode(context, maxMessageSize, this);

            ConvertToOpcUaUniversalTime(networkMessage);
            var m = PubSubMessage
                .Decode(CreateReader(buffers), networkMessage.ContentType, context, this)
                .OfType<UadpNetworkMessage>()
                .ToList();
            var result = m.SelectMany(m => m.Messages).Select(m => m.Payload).ToList();
            Assert.All(networkMessage.Messages.Select(m => m.Payload), (p, i) => Assert.True(result[i].Equals(p)));
        }

        [Theory]
        [InlineData(MessageType.KeyFrame, 1, 100)]
        [InlineData(MessageType.KeyFrame, 5, 100)]
        [InlineData(MessageType.KeyFrame, 194, 100)]
        [InlineData(MessageType.KeyFrame, 1, 1024)]
        [InlineData(MessageType.KeyFrame, 5, 1024)]
        [InlineData(MessageType.KeyFrame, 194, 1024)]
        [InlineData(MessageType.KeyFrame, 1, 256 * 1024)]
        [InlineData(MessageType.KeyFrame, 5, 256 * 1024)]
        [InlineData(MessageType.KeyFrame, 194, 256 * 1024)]
        [InlineData(MessageType.DeltaFrame, 1, 100)]
        [InlineData(MessageType.DeltaFrame, 5, 100)]
        [InlineData(MessageType.DeltaFrame, 194, 100)]
        [InlineData(MessageType.DeltaFrame, 1, 1024)]
        [InlineData(MessageType.DeltaFrame, 5, 1024)]
        [InlineData(MessageType.DeltaFrame, 194, 1024)]
        [InlineData(MessageType.DeltaFrame, 1, 256 * 1024)]
        [InlineData(MessageType.DeltaFrame, 5, 256 * 1024)]
        [InlineData(MessageType.DeltaFrame, 194, 256 * 1024)]
        [InlineData(MessageType.Event, 1, 100)]
        [InlineData(MessageType.Event, 194, 100)]
        [InlineData(MessageType.Event, 1, 1024)]
        [InlineData(MessageType.Event, 194, 1024)]
        [InlineData(MessageType.Event, 194, 256 * 1024)]
        public void EncodeDecodeNetworkMessagesNoHeaders(MessageType type, int numberOfMessages, int maxMessageSize)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(type, sequenceNumber))
                .ToList();
            var networkMessage = CreateNetworkMessage(NetworkMessageContentMaskDefault
                & ~(NetworkMessageContentFlags.GroupHeader | NetworkMessageContentFlags.PayloadHeader), messages);

            var context = new ServiceMessageContext();
            var buffers = networkMessage.Encode(context, maxMessageSize, this);

            ConvertToOpcUaUniversalTime(networkMessage);

            var m = PubSubMessage
                .Decode(CreateReader(buffers), networkMessage.ContentType, context, this)
                .OfType<UadpNetworkMessage>()
                .ToList();
            var result = m.SelectMany(m => m.Messages).Select(m => m.Payload).ToList();
            Assert.All(networkMessage.Messages.Select(m => m.Payload), (p, i) => Assert.True(result[i].Equals(p)));
        }

        [Theory]
        [InlineData(MessageType.KeyFrame, 1, 100)]
        [InlineData(MessageType.KeyFrame, 5, 100)]
        [InlineData(MessageType.KeyFrame, 194, 100)]
        [InlineData(MessageType.KeyFrame, 1, 1024)]
        [InlineData(MessageType.KeyFrame, 5, 1024)]
        [InlineData(MessageType.KeyFrame, 194, 1024)]
        [InlineData(MessageType.KeyFrame, 1, 256 * 1024)]
        [InlineData(MessageType.KeyFrame, 5, 256 * 1024)]
        [InlineData(MessageType.KeyFrame, 194, 256 * 1024)]
        [InlineData(MessageType.DeltaFrame, 1, 100)]
        [InlineData(MessageType.DeltaFrame, 5, 100)]
        [InlineData(MessageType.DeltaFrame, 194, 100)]
        [InlineData(MessageType.DeltaFrame, 1, 1024)]
        [InlineData(MessageType.DeltaFrame, 5, 1024)]
        [InlineData(MessageType.DeltaFrame, 194, 1024)]
        [InlineData(MessageType.DeltaFrame, 1, 256 * 1024)]
        [InlineData(MessageType.DeltaFrame, 5, 256 * 1024)]
        [InlineData(MessageType.DeltaFrame, 194, 256 * 1024)]
        [InlineData(MessageType.Event, 1, 100)]
        [InlineData(MessageType.Event, 194, 100)]
        [InlineData(MessageType.Event, 1, 1024)]
        [InlineData(MessageType.Event, 194, 1024)]
        [InlineData(MessageType.Event, 194, 256 * 1024)]
        public void EncodeDecodeNetworkMessagesNoHeaderRaw(MessageType type, int numberOfMessages, int maxMessageSize)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(type, sequenceNumber,
                    dataSetFieldContentMask: DataSetFieldContentFlags.RawData))
                .ToList();
            var networkMessage = CreateNetworkMessage(NetworkMessageContentMaskDefault
                & ~(NetworkMessageContentFlags.GroupHeader | NetworkMessageContentFlags.PayloadHeader), messages);

            var context = new ServiceMessageContext();
            var buffers = networkMessage.Encode(context, maxMessageSize, this);

            ConvertToOpcUaUniversalTime(networkMessage);

            // Compare payload as raw data equivalent
            var serializer = new NewtonsoftJsonSerializer();

            var decodedMessages = PubSubMessage
                .Decode(CreateReader(buffers), networkMessage.ContentType, context, this)
                .OfType<UadpNetworkMessage>()
                .SelectMany(m => m.Messages)
                .ToList();

            var result = serializer.Parse(serializer.SerializeToString(decodedMessages
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
        private UadpNetworkMessage CreateNetworkMessage(
            NetworkMessageContentFlags contentMask, List<BaseDataSetMessage> messages)
        {
            return new UadpNetworkMessage
            {
                Messages = messages,
                WriterGroupId = 4,
                Timestamp = DateTimeOffset.UtcNow,
                PicoSeconds = 65,
                SequenceNumber = () => _lastSequenceNumber++,
                DataSetClassId = Guid.NewGuid(),
                PublisherId = "PublisherId",
                NetworkMessageContentMask = contentMask
            };
        }
        private ushort _lastSequenceNumber;

        /// <summary>
        /// Create dataset message
        /// </summary>
        /// <param name="type"></param>
        /// <param name="sequenceNumber"></param>
        /// <param name="dataSetMessageContentMask"></param>
        /// <param name="dataSetFieldContentMask"></param>
        private static UadpDataSetMessage CreateDataSetMessage(MessageType type, int sequenceNumber,
            DataSetMessageContentFlags dataSetMessageContentMask = DataSetMessageContentMaskDefault,
            DataSetFieldContentFlags dataSetFieldContentMask = DataSetFieldContentFlagsDefault)
        {
            return new UadpDataSetMessage
            {
                DataSetWriterId = 3,
                MetaDataVersion = new ConfigurationVersionDataType { MajorVersion = 1, MinorVersion = 1 },
                SequenceNumber = (ushort)sequenceNumber,
                Status = StatusCodes.Bad,
                Timestamp = DateTimeOffset.UtcNow,
                MessageType = type,
                Picoseconds = 1,
                Payload = CreateDataSet(type == MessageType.DeltaFrame, dataSetFieldContentMask),
                DataSetMessageContentMask = dataSetMessageContentMask
            };
        }

        public PublishedDataSetMetaDataModel Find(ushort writerId, uint majorVersion = 0, uint minorVersion = 0)
        {
            // Return independent on whether we receive valid writer id or major minor versions
            // In raw mode without payload headers there is no way to decode key frames without
            // preconfigured writer id and versioning, so we assume we have it by some means
            return new PublishedDataSetMetaDataModel
            {
                DataSetMetaData = new DataSetMetaDataModel(),
                Fields = new[] {
                    new PublishedFieldMetaDataModel { Name = "1", BuiltInType = (byte)BuiltInType.Int32, ValueRank = ValueRanks.Scalar },
                    new PublishedFieldMetaDataModel { Name = "2", BuiltInType = (byte)BuiltInType.Float, ValueRank = ValueRanks.Scalar },
                    new PublishedFieldMetaDataModel { Name = "3", BuiltInType = (byte)BuiltInType.String, ValueRank = ValueRanks.Scalar }
                }
            };
        }

        /// <summary>
        /// Create dataset
        /// </summary>
        /// <param name="deltaFrame"></param>
        /// <param name="dataSetFieldContentMask"></param>
        private static DataSet CreateDataSet(bool deltaFrame,
            DataSetFieldContentFlags dataSetFieldContentMask = DataSetFieldContentFlagsDefault)
        {
            return !deltaFrame ? new DataSet(new Dictionary<string, DataValue>
            {
                { "1", new DataValue(new Variant(5), StatusCodes.Good, DateTime.Now, DateTime.UtcNow) },
                { "2", new DataValue(new Variant(0.5), StatusCodes.Good, DateTime.Now) },
                { "3", new DataValue("abcd") }
            }, dataSetFieldContentMask) : new DataSet(new Dictionary<string, DataValue>
            {
                { "3", new DataValue("abcd") }
            }, dataSetFieldContentMask);
        }

        private static Queue<ReadOnlySequence<byte>> CreateReader(IReadOnlyList<ReadOnlySequence<byte>> buffers)
        {
            var reader = new Queue<ReadOnlySequence<byte>>();
            foreach (var buffer in buffers)
            {
                reader.Enqueue(buffer);
            }
            return reader;
        }
    }
}
