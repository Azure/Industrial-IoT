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

    public class PubSubJsonEncoderDecoder {

        private uint _currentSequenceNumber = 0;

        [Fact]
        public void EncodeDecodeNetworkMessage() {
            var payload = new Dictionary<string, DataValue> {
                { "1", new DataValue(new Variant(5), StatusCodes.Good, DateTime.Now, DateTime.UtcNow) },
                { "2", new DataValue(new Variant(0.5), StatusCodes.Good, DateTime.Now) },
                { "3", new DataValue("abcd") }
            };

            var message = new DataSetMessage {
                DataSetWriterId = "WriterId",
                MetaDataVersion = new ConfigurationVersionDataType { MajorVersion = 1, MinorVersion = 1 },
                SequenceNumber = ++_currentSequenceNumber,
                Status = StatusCodes.Good,
                Timestamp = DateTime.UtcNow,
                MessageContentMask = (uint)(
                    JsonDataSetMessageContentMask.DataSetWriterId |
                    JsonDataSetMessageContentMask.SequenceNumber |
                    JsonDataSetMessageContentMask.MetaDataVersion |
                    JsonDataSetMessageContentMask.Timestamp |
                    JsonDataSetMessageContentMask.Status),
                Payload = new DataSet(payload, (uint)(
                    DataSetFieldContentMask.StatusCode | 
                    DataSetFieldContentMask.SourceTimestamp))
            };

            var networkMessage = new NetworkMessage {
                MessageId = Guid.NewGuid().ToString(), // TODO
                MessageType = "ua-data",
                Messages = new List<DataSetMessage>(),
                PublisherId = "PublisherId"
            };

            networkMessage.Messages.Add(message);
            networkMessage.MessageContentMask = (uint)(
                JsonNetworkMessageContentMask.PublisherId |
                JsonNetworkMessageContentMask.NetworkMessageHeader |
                JsonNetworkMessageContentMask.SingleDataSetMessage);

            byte[] buffer;
            var context = new ServiceMessageContext();
            using (var stream = new MemoryStream()) {
                using (var encoder = new JsonEncoderEx(stream, context)) {
                    networkMessage.Encode(encoder);
                }
                buffer = stream.ToArray();
            }

            ConvertToOpcUaUniversalTime(networkMessage);

            using (var stream = new MemoryStream(buffer)) {
                using (var decoder = new JsonDecoderEx(stream, context)) {
                    var result = decoder.ReadEncodeable(null, typeof(NetworkMessage)) as NetworkMessage;
                    Assert.Equal(networkMessage, result);
                }
            }
        }


        [Fact]
        public void EncodeDecodeNetworkMessageFull() {

            var payload = new Dictionary<string, DataValue> {
                ["abcd"] = new DataValue(new Variant(1234), StatusCodes.Good, DateTime.Now, DateTime.UtcNow),
                ["http://microsoft.com"] = new DataValue(new Variant(-222222222), StatusCodes.Bad, DateTime.MinValue, DateTime.Now),
                ["1111111111111111111111111"] = new DataValue(new Variant(false), StatusCodes.Bad, DateTime.UtcNow, DateTime.MinValue),
                ["@#$%^&*()_+~!@#$%^*(){}"] = new DataValue(new Variant(new byte[] { 0, 2, 4, 6}), StatusCodes.Good),
                ["1245"] = new DataValue(new Variant("hello"), StatusCodes.Bad, DateTime.Now, DateTime.MinValue),
                ["..."] = new DataValue(new Variant(new Variant("imbricated")))
            };

            var message = new DataSetMessage {
                DataSetWriterId = "WriterId",
                MetaDataVersion = new ConfigurationVersionDataType { MajorVersion = 1, MinorVersion = 1 },
                SequenceNumber = ++_currentSequenceNumber,
                Status = StatusCodes.Good,
                Timestamp = DateTime.UtcNow,
                Picoseconds = 1234,
                MessageContentMask = (uint)(
                    JsonDataSetMessageContentMask.DataSetWriterId |
                    JsonDataSetMessageContentMask.SequenceNumber |
                    JsonDataSetMessageContentMask.MetaDataVersion |
                    JsonDataSetMessageContentMask.Timestamp |
                    JsonDataSetMessageContentMask.Status),
                Payload = new DataSet(payload, (uint)(
                    DataSetFieldContentMask.StatusCode |
                    DataSetFieldContentMask.SourceTimestamp |
                    DataSetFieldContentMask.ServerTimestamp |
                    DataSetFieldContentMask.SourcePicoSeconds |
                    DataSetFieldContentMask.ServerPicoSeconds))
            };

            var networkMessage = new NetworkMessage {
                MessageId = Guid.NewGuid().ToString(), // TODO
                MessageType = "ua-data",
                Messages = new List<DataSetMessage>(),
                PublisherId = "PublisherId",
                DataSetClassId = "1234"
            };

            networkMessage.Messages.Add(message);
            networkMessage.MessageContentMask = (uint)(
                JsonNetworkMessageContentMask.PublisherId |
                JsonNetworkMessageContentMask.NetworkMessageHeader |
                JsonNetworkMessageContentMask.SingleDataSetMessage |
                JsonNetworkMessageContentMask.DataSetClassId);

            byte[] buffer;
            var context = new ServiceMessageContext();
            using (var stream = new MemoryStream()) {
                using (var encoder = new JsonEncoderEx(stream, context)) {
                    networkMessage.Encode(encoder);
                }
                buffer = stream.ToArray();
            }

            ConvertToOpcUaUniversalTime(networkMessage);

            using (var stream = new MemoryStream(buffer)) {
                using (var decoder = new JsonDecoderEx(stream, context)) {
                    var result = decoder.ReadEncodeable(null, typeof(NetworkMessage)) as NetworkMessage;
                    Assert.Equal(networkMessage, result);
                }
            }
        }

        /// <summary>
        /// Convert timestamps of payload to OpcUa Utc.
        /// </summary>
        private void ConvertToOpcUaUniversalTime(NetworkMessage networkMessage) {
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
