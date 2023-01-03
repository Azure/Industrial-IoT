// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.PubSub.Tests {
    using System;
    using System.IO;
    using System.Collections.Generic;
    using Opc.Ua.Encoders;
    using Xunit;
    using Opc.Ua.Extensions;
    using Microsoft.Azure.IIoT.OpcUa.Core;
    using System.Linq;

    public class PubSubJsonEncoderDecoder {

        [Theory]
        [InlineData(false, JsonNetworkMessageContentMask.SingleDataSetMessage, 1)]
        [InlineData(false, 0, 3)]
        [InlineData(false, 0, 1)]
        [InlineData(true, JsonNetworkMessageContentMask.SingleDataSetMessage, 1)]
        [InlineData(true, 0, 3)]
        public void EncodeDecodeNetworkMessage(bool useCompatibilityMode, uint extraNetworkMessage, int numberOfMessages) {
            var payload = new Dictionary<string, DataValue> {
                { "1", new DataValue(new Variant(5), StatusCodes.Good, DateTime.Now, DateTime.UtcNow) },
                { "2", new DataValue(new Variant(0.5), StatusCodes.Good, DateTime.Now) },
                { "3", new DataValue("abcd") }
            };

            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)new JsonDataSetMessage {
                    DataSetWriterName = "WriterId",
                    DataSetWriterId = (ushort)(useCompatibilityMode ? 0 : 3),
                    MetaDataVersion = new ConfigurationVersionDataType { MajorVersion = 1, MinorVersion = 1 },
                    SequenceNumber = (ushort)sequenceNumber,
                    Status = StatusCodes.Bad,
                    Timestamp = DateTime.UtcNow,
                    UseCompatibilityMode = useCompatibilityMode,
                    MessageType = MessageType.KeyFrame,
                    Picoseconds = 1,
                    DataSetMessageContentMask = (uint)(
                        JsonDataSetMessageContentMask2.DataSetWriterName |
                        JsonDataSetMessageContentMask.MessageType |
                        JsonDataSetMessageContentMask.DataSetWriterId |
                        JsonDataSetMessageContentMask.SequenceNumber |
                        JsonDataSetMessageContentMask.MetaDataVersion |
                        JsonDataSetMessageContentMask.Timestamp |
                        JsonDataSetMessageContentMask.Status),
                    Payload = new DataSet(payload, (uint)(
                        DataSetFieldContentMask.SourceTimestamp |
                        DataSetFieldContentMask.ServerTimestamp |
                        DataSetFieldContentMask.SourcePicoSeconds |
                        DataSetFieldContentMask.ServerPicoSeconds |
                        DataSetFieldContentMask.StatusCode))
                })
                .ToList();

            var networkMessage = new JsonNetworkMessage {
                MessageId = Guid.NewGuid().ToString(), // TODO
                Messages = messages,
                DataSetWriterGroup = "group",
                DataSetClassId = Guid.NewGuid(),
                PublisherId = "PublisherId",
                NetworkMessageContentMask = (uint)(
                    JsonNetworkMessageContentMask.PublisherId |
                    JsonNetworkMessageContentMask.NetworkMessageHeader |
                    JsonNetworkMessageContentMask.DataSetMessageHeader |
                    JsonNetworkMessageContentMask.DataSetClassId |
                    (JsonNetworkMessageContentMask)extraNetworkMessage)
            };

            var context = new ServiceMessageContext();
            var buffer = Assert.Single(networkMessage.Encode(context, 256 * 1000));

            ConvertToOpcUaUniversalTime(networkMessage);

            var result = PubSubMessage.Decode(buffer, MessageSchemaTypes.NetworkMessageJson, context);
            Assert.Equal(networkMessage, result);
        }

        [Theory]
        [InlineData(false, JsonNetworkMessageContentMask.SingleDataSetMessage, 5)]
        [InlineData(false, 0, 10)]
        [InlineData(true, 0, 15)]
        [InlineData(true, JsonNetworkMessageContentMask.SingleDataSetMessage, 5)]
        public void EncodeDecodeNetworkMessages(bool useCompatibilityMode, uint extraNetworkMessage, int numberOfMessages) {
            var payload = new Dictionary<string, DataValue> {
                { "1", new DataValue(new Variant(5), StatusCodes.Good, DateTime.Now, DateTime.UtcNow) },
                { "2", new DataValue(new Variant(0.5), StatusCodes.Good, DateTime.Now) },
                { "3", new DataValue("abcd") }
            };

            var messages = Enumerable
                .Range(3, numberOfMessages)
                .Select(sequenceNumber => (BaseDataSetMessage)new JsonDataSetMessage {
                    DataSetWriterName = "WriterId",
                    DataSetWriterId = (ushort)(useCompatibilityMode ? 0 : 3),
                    MetaDataVersion = new ConfigurationVersionDataType { MajorVersion = 1, MinorVersion = 1 },
                    SequenceNumber = (ushort)sequenceNumber,
                    Status = StatusCodes.Bad,
                    Timestamp = DateTime.UtcNow,
                    UseCompatibilityMode = useCompatibilityMode,
                    MessageType = MessageType.KeyFrame,
                    Picoseconds = 1,
                    DataSetMessageContentMask = (uint)(
                        JsonDataSetMessageContentMask2.DataSetWriterName |
                        JsonDataSetMessageContentMask.MessageType |
                        JsonDataSetMessageContentMask.DataSetWriterId |
                        JsonDataSetMessageContentMask.SequenceNumber |
                        JsonDataSetMessageContentMask.MetaDataVersion |
                        JsonDataSetMessageContentMask.Timestamp |
                        JsonDataSetMessageContentMask.Status),
                    Payload = new DataSet(payload, (uint)(
                        DataSetFieldContentMask.SourceTimestamp |
                        DataSetFieldContentMask.ServerTimestamp |
                        DataSetFieldContentMask.SourcePicoSeconds |
                        DataSetFieldContentMask.ServerPicoSeconds |
                        DataSetFieldContentMask.StatusCode))
                })
                .ToList();

            var networkMessage = new JsonNetworkMessage {
                MessageId = Guid.NewGuid().ToString(), // TODO
                Messages = messages,
                DataSetWriterGroup = "group",
                DataSetClassId = Guid.NewGuid(),
                PublisherId = "PublisherId",
                NetworkMessageContentMask = (uint)(
                    JsonNetworkMessageContentMask.PublisherId |
                    JsonNetworkMessageContentMask.NetworkMessageHeader |
                    JsonNetworkMessageContentMask.DataSetMessageHeader |
                    JsonNetworkMessageContentMask.DataSetClassId |
                    (JsonNetworkMessageContentMask)extraNetworkMessage)
            };

            var context = new ServiceMessageContext();
            var buffers = networkMessage.Encode(context, 1024);
            Assert.Equal(numberOfMessages, buffers.Count);

            ConvertToOpcUaUniversalTime(networkMessage);

            var m = buffers
                .Select(buffer => (BaseNetworkMessage)PubSubMessage
                    .Decode(buffer, MessageSchemaTypes.NetworkMessageJson, context))
                .ToList();
            var result = m[0];
            result.Messages = m.SelectMany(m => m.Messages).ToList();
            Assert.Equal(networkMessage, result);
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
    }
}
