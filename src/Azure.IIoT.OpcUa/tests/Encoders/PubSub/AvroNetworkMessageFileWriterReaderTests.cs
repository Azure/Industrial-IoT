﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub
{
    using Azure.IIoT.OpcUa.Encoders.Models;
    using Azure.IIoT.OpcUa.Encoders.Schemas;
    using Furly.Extensions.Logging;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Opc.Ua;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Xunit;

    /// <summary>
    /// Avro file reader/writer tests
    /// </summary>
    public class AvroNetworkMessageFileWriterReaderTests
    {
        public const AvroNetworkMessageContentMask NetworkMessageContentMaskDefault =
            AvroNetworkMessageContentMask.NetworkMessageHeader |
            AvroNetworkMessageContentMask.DataSetMessageHeader;

        public const DataSetFieldContentMask DataSetFieldContentMaskDefault =
            DataSetFieldContentMask.SourceTimestamp |
            DataSetFieldContentMask.ServerTimestamp |
            DataSetFieldContentMask.SourcePicoSeconds |
            DataSetFieldContentMask.ServerPicoSeconds |
            DataSetFieldContentMask.StatusCode;

        [Theory]
        [InlineData(false, NetworkMessageContentMaskDefault | AvroNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(false, NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(false, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(false, NetworkMessageContentMaskDefault | AvroNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        [InlineData(false, NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(false, NetworkMessageContentMaskDefault, 15, 1024)]
        [InlineData(true, NetworkMessageContentMaskDefault | AvroNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(true, NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(true, NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(true, NetworkMessageContentMaskDefault | AvroNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        [InlineData(true, NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(true, NetworkMessageContentMaskDefault, 15, 1024)]
        public void ReadWriteNetworkMessages(bool compress,
            AvroNetworkMessageContentMask contentMask, int numberOfMessages, int maxMessageSize)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(sequenceNumber))
                .ToList();
            var networkMessage = CreateNetworkMessage(contentMask, messages);
            networkMessage.UseGzipCompression = compress;

            var context = new ServiceMessageContext();
            var buffers = networkMessage.Encode(context, maxMessageSize);

            using var file = new MemoryStream();
            using (var writer = AvroFileWriter.AvroFile.CreateFromStream(
                file, networkMessage.Schema.ToJson(), new Dictionary<string, string>(), Log.Console("test"), compress))
            {
                writer.Write(buffers);
            }

            ConvertToOpcUaUniversalTime(networkMessage);
            networkMessage.UseGzipCompression = false; // Unset compression flag now as we are reading the file

            file.Seek(0, SeekOrigin.Begin);
            using (var reader = new AvroFileReader(file))
            {
                var m = reader
                    .Stream((schema, stream) => PubSubMessage.Decode(stream, networkMessage.ContentType,
                        context, messageSchema: schema.ToJson()).ToList())
                    .SelectMany(m => m.Cast<BaseNetworkMessage>())
                    .ToList();

                var result = m[0];
                result.Messages = m.SelectMany(m => m.Messages).ToList();
                Assert.Equal(networkMessage, result);
            }
        }

        [Theory]
        [InlineData(NetworkMessageContentMaskDefault | AvroNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(NetworkMessageContentMaskDefault | AvroNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 15, 1024)]
        public void ReadWriteNetworkMessagesNoNetworkMessageHeader(
            AvroNetworkMessageContentMask contentMask, int numberOfMessages, int maxMessageSize)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(sequenceNumber))
                .ToList();
            var networkMessage = CreateNetworkMessage(contentMask & ~AvroNetworkMessageContentMask.NetworkMessageHeader, messages);

            var context = new ServiceMessageContext();
            var buffers = networkMessage.Encode(context, maxMessageSize);

            using var file = new MemoryStream();
            using (var writer = AvroFileWriter.AvroFile.CreateFromStream(
                file, networkMessage.Schema.ToJson(), new Dictionary<string, string>(), Log.Console("test")))
            {
                writer.Write(buffers);
            }

            ConvertToOpcUaUniversalTime(networkMessage);
            file.Seek(0, SeekOrigin.Begin);
            using (var reader = new AvroFileReader(file))
            {
                var result = reader
                    .Stream((schema, stream) => PubSubMessage.Decode(stream, networkMessage.ContentType,
                        context, messageSchema: schema.ToJson()).ToList())
                    .SelectMany(m => m.Cast<BaseNetworkMessage>().ToList())
                    .SelectMany(m => m.Messages)
                    .ToList();

                Assert.Equal(networkMessage.Messages, result);
            }
        }

        [Theory]
        [InlineData(NetworkMessageContentMaskDefault | AvroNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 15, 256 * 1024)]
        [InlineData(NetworkMessageContentMaskDefault | AvroNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 15, 1024)]
        public void ReadWriteNetworkMessagesNoDataSetMessageHeader(
            AvroNetworkMessageContentMask contentMask, int numberOfMessages, int maxMessageSize)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(sequenceNumber))
                .ToList();
            var networkMessage = CreateNetworkMessage(contentMask
                & ~AvroNetworkMessageContentMask.DataSetMessageHeader, messages);

            var context = new ServiceMessageContext();
            var buffers = networkMessage.Encode(context, maxMessageSize);

            using var file = new MemoryStream();
            using (var writer = AvroFileWriter.AvroFile.CreateFromStream(
                file, networkMessage.Schema.ToJson(), new Dictionary<string, string>(), Log.Console("test")))
            {
                writer.Write(buffers);
            }
            ConvertToOpcUaUniversalTime(networkMessage);
            file.Seek(0, SeekOrigin.Begin);
            using (var reader = new AvroFileReader(file))
            {
                var result = reader
                    .Stream((schema, stream) => PubSubMessage.Decode(stream, networkMessage.ContentType,
                        context, messageSchema: schema.ToJson()).ToList())
                    .SelectMany(m => m.Cast<BaseNetworkMessage>().ToList())
                    .SelectMany(m => m.Messages).Select(m => m.Payload)
                    .ToList();

                Assert.All(networkMessage.Messages.Select(m => m.Payload), (p, i) => Assert.True(result[i].Equals(p)));
            }
        }

        [Theory]
        [InlineData(NetworkMessageContentMaskDefault | AvroNetworkMessageContentMask.SingleDataSetMessage, 5, 256 * 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 10, 256 * 1024)]
        [InlineData(NetworkMessageContentMaskDefault | AvroNetworkMessageContentMask.SingleDataSetMessage, 5, 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 10, 1024)]
        [InlineData(NetworkMessageContentMaskDefault, 15, 1024)]
        public void ReadWriteNetworkMessagesNoHeader(
            AvroNetworkMessageContentMask contentMask, int numberOfMessages, int maxMessageSize)
        {
            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)CreateDataSetMessage(sequenceNumber))
                .ToList();
            var networkMessage = CreateNetworkMessage(contentMask
                & ~(AvroNetworkMessageContentMask.NetworkMessageHeader | AvroNetworkMessageContentMask.DataSetMessageHeader), messages);

            var context = new ServiceMessageContext();
            var buffers = networkMessage.Encode(context, maxMessageSize);
            using var file = new MemoryStream();
            using (var writer = AvroFileWriter.AvroFile.CreateFromStream(
                file, networkMessage.Schema.ToJson(), new Dictionary<string, string>(), Log.Console("test")))
            {
                writer.Write(buffers);
            }

            ConvertToOpcUaUniversalTime(networkMessage);
            file.Seek(0, SeekOrigin.Begin);
            using (var reader = new AvroFileReader(file))
            {
                var result = reader
                    .Stream((schema, stream) => PubSubMessage.Decode(stream, networkMessage.ContentType,
                         context, messageSchema: schema.ToJson()).ToList())
                    .SelectMany(m => m.Cast<BaseNetworkMessage>())
                    .SelectMany(m => m.Messages.Select(m => m.Payload))
                    .ToList();

                var expected = networkMessage.Messages.Select(m => m.Payload).ToList();
                Assert.All(expected, (p, i) => Assert.True(result[i].Equals(p)));
            }
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
            AvroNetworkMessageContentMask contentMask, List<BaseDataSetMessage> messages)
        {
            return new AvroNetworkMessage
            {
                MessageId = () => "9279C0B3-DA88-45A4-AF74-451CEBF82DB0",
                Messages = messages,
                DataSetWriterGroup = "group",
                DataSetClassId = Guid.NewGuid(),
                PublisherId = "PublisherId",
                EmitConciseSchema = true,
                NetworkMessageContentMask = (uint)contentMask
            };
        }

        /// <summary>
        /// Create dataset message
        /// </summary>
        /// <param name="sequenceNumber"></param>
        /// <param name="dataSetFieldContentMask"></param>
        private static AvroDataSetMessage CreateDataSetMessage(int sequenceNumber,
            DataSetFieldContentMask dataSetFieldContentMask = DataSetFieldContentMaskDefault)
        {
            return new AvroDataSetMessage
            {
                DataSetWriterName = "WriterId",
                DataSetWriterId = 3,
                MetaDataVersion = new ConfigurationVersionDataType { MajorVersion = 1, MinorVersion = 1 },
                SequenceNumber = (ushort)sequenceNumber,
                Status = StatusCodes.Bad,
                Timestamp = DateTime.UtcNow,
                MessageType = MessageType.KeyFrame,
                Picoseconds = 1,
                Payload = CreateDataSet(dataSetFieldContentMask)
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
