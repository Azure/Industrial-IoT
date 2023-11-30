// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub.Tests
{
    using Azure.IIoT.OpcUa.Encoders.Models;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Opc.Ua;
    using System;
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
        public const UadpNetworkMessageContentMask NetworkMessageContentMaskDefault =
            UadpNetworkMessageContentMask.PublisherId |
            UadpNetworkMessageContentMask.GroupHeader |
            UadpNetworkMessageContentMask.WriterGroupId |
            UadpNetworkMessageContentMask.GroupVersion |
            UadpNetworkMessageContentMask.NetworkMessageNumber |
            UadpNetworkMessageContentMask.SequenceNumber |
            UadpNetworkMessageContentMask.PayloadHeader |
            UadpNetworkMessageContentMask.Timestamp |
            UadpNetworkMessageContentMask.PicoSeconds |
            UadpNetworkMessageContentMask.DataSetClassId |
            UadpNetworkMessageContentMask.PromotedFields;

        public const UadpDataSetMessageContentMask DataSetMessageContentMaskDefault =
            UadpDataSetMessageContentMask.PicoSeconds |
            UadpDataSetMessageContentMask.SequenceNumber |
            UadpDataSetMessageContentMask.MajorVersion |
            UadpDataSetMessageContentMask.MinorVersion |
            UadpDataSetMessageContentMask.Timestamp |
            UadpDataSetMessageContentMask.Status;

        public const DataSetFieldContentMask DataSetFieldContentMaskDefault =
            DataSetFieldContentMask.SourceTimestamp |
            DataSetFieldContentMask.ServerTimestamp |
            DataSetFieldContentMask.SourcePicoSeconds |
            DataSetFieldContentMask.ServerPicoSeconds |
            DataSetFieldContentMask.StatusCode;

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
                NetworkMessageContentMaskDefault & ~UadpNetworkMessageContentMask.GroupHeader, messages);

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
                NetworkMessageContentMaskDefault & ~UadpNetworkMessageContentMask.PayloadHeader, messages);

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
                & ~(UadpNetworkMessageContentMask.GroupHeader | UadpNetworkMessageContentMask.PayloadHeader), messages);

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
                    dataSetFieldContentMask: DataSetFieldContentMask.RawData))
                .ToList();
            var networkMessage = CreateNetworkMessage(NetworkMessageContentMaskDefault
                & ~(UadpNetworkMessageContentMask.GroupHeader | UadpNetworkMessageContentMask.PayloadHeader), messages);

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
        private UadpNetworkMessage CreateNetworkMessage(
            UadpNetworkMessageContentMask contentMask, List<BaseDataSetMessage> messages)
        {
            return new UadpNetworkMessage
            {
                Messages = messages,
                WriterGroupId = 4,
                Timestamp = DateTime.UtcNow,
                PicoSeconds = 65,
                SequenceNumber = () => _lastSequenceNumber++,
                DataSetClassId = Guid.NewGuid(),
                PublisherId = "PublisherId",
                NetworkMessageContentMask = (uint)contentMask
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
            UadpDataSetMessageContentMask dataSetMessageContentMask = DataSetMessageContentMaskDefault,
            DataSetFieldContentMask dataSetFieldContentMask = DataSetFieldContentMaskDefault)
        {
            return new UadpDataSetMessage
            {
                DataSetWriterId = 3,
                MetaDataVersion = new ConfigurationVersionDataType { MajorVersion = 1, MinorVersion = 1 },
                SequenceNumber = (ushort)sequenceNumber,
                Status = StatusCodes.Bad,
                Timestamp = DateTime.UtcNow,
                MessageType = type,
                Picoseconds = 1,
                Payload = CreateDataSet(type == MessageType.DeltaFrame, dataSetFieldContentMask),
                DataSetMessageContentMask = (uint)dataSetMessageContentMask
            };
        }

        public DataSetMetaDataType Find(ushort writerId, uint majorVersion = 0, uint minorVersion = 0)
        {
            // Return independent on whether we receive valid writer id or major minor versions
            // In raw mode without payload headers there is no way to decode key frames without
            // preconfigured writer id and versioning, so we assume we have it by some means
            return new DataSetMetaDataType
            {
                Fields = new FieldMetaDataCollection {
                    new FieldMetaData { Name = "1", BuiltInType = (byte)BuiltInType.Int32, ValueRank = ValueRanks.Scalar },
                    new FieldMetaData { Name = "2", BuiltInType = (byte)BuiltInType.Float, ValueRank = ValueRanks.Scalar },
                    new FieldMetaData { Name = "3", BuiltInType = (byte)BuiltInType.String, ValueRank = ValueRanks.Scalar }
                }
            };
        }

        /// <summary>
        /// Create dataset
        /// </summary>
        /// <param name="deltaFrame"></param>
        /// <param name="dataSetFieldContentMask"></param>
        private static DataSet CreateDataSet(bool deltaFrame,
            DataSetFieldContentMask dataSetFieldContentMask = DataSetFieldContentMaskDefault)
        {
            return !deltaFrame ? new DataSet(new Dictionary<string, DataValue> {
                { "1", new DataValue(new Variant(5), StatusCodes.Good, DateTime.Now, DateTime.UtcNow) },
                { "2", new DataValue(new Variant(0.5), StatusCodes.Good, DateTime.Now) },
                { "3", new DataValue("abcd") }
            }, (uint)dataSetFieldContentMask) : new DataSet(new Dictionary<string, DataValue> {
                { "3", new DataValue("abcd") }
            }, (uint)dataSetFieldContentMask);
        }

        private static Queue<ReadOnlyMemory<byte>> CreateReader(IReadOnlyList<ReadOnlyMemory<byte>> buffers)
        {
            var reader = new Queue<ReadOnlyMemory<byte>>();
            foreach (var buffer in buffers)
            {
                reader.Enqueue(buffer);
            }
            return reader;
        }
    }
}
