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

            var message = new JsonDataSetMessage {
                DataSetWriterName = "WriterId",
                DataSetWriterId = 3,
                MetaDataVersion = new ConfigurationVersionDataType { MajorVersion = 1, MinorVersion = 1 },
                SequenceNumber = ++_currentSequenceNumber,
                Status = StatusCodes.Bad,
                Timestamp = DateTime.UtcNow,
                DataSetMessageContentMask = (uint)(
                    JsonDataSetMessageContentMask2.DataSetWriterName |
                    JsonDataSetMessageContentMask.DataSetWriterId |
                    JsonDataSetMessageContentMask.SequenceNumber |
                    JsonDataSetMessageContentMask.MetaDataVersion |
                    JsonDataSetMessageContentMask.Timestamp |
                    JsonDataSetMessageContentMask.Status),
                Payload = new DataSet(payload, (uint)(
                    DataSetFieldContentMask.StatusCode |
                    DataSetFieldContentMask.SourceTimestamp))
            };

            var networkMessage = new JsonNetworkMessage {
                MessageId = Guid.NewGuid().ToString(), // TODO
                Messages = new List<BaseDataSetMessage>(),
                PublisherId = "PublisherId"
            };

            networkMessage.Messages.Add(message);
            networkMessage.NetworkMessageContentMask = (uint)(
                JsonNetworkMessageContentMask.PublisherId |
                JsonNetworkMessageContentMask.NetworkMessageHeader |
                JsonNetworkMessageContentMask.SingleDataSetMessage);

            var context = new ServiceMessageContext();
            var buffer = Assert.Single(networkMessage.Encode(context, 1024));

            ConvertToOpcUaUniversalTime(networkMessage);

            using (var stream = new MemoryStream(buffer)) {
                using (var decoder = new JsonDecoderEx(stream, context)) {
                    var result = decoder.ReadEncodeable(null, typeof(JsonNetworkMessage)) as JsonNetworkMessage;
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

            var message = new JsonDataSetMessage {
                DataSetWriterName = "WriterId",
                DataSetWriterId = 1,
                MetaDataVersion = new ConfigurationVersionDataType { MajorVersion = 1, MinorVersion = 1 },
                SequenceNumber = ++_currentSequenceNumber,
                Status = StatusCodes.GoodCallAgain,
                Timestamp = DateTime.UtcNow,
                Picoseconds = 1234,
                DataSetMessageContentMask = (uint)(
                    JsonDataSetMessageContentMask2.DataSetWriterName |
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

            var networkMessage = new JsonNetworkMessage {
                MessageId = Guid.NewGuid().ToString(), // TODO
                Messages = new List<BaseDataSetMessage>(),
                PublisherId = "PublisherId",
                DataSetClassId = Guid.NewGuid()
            };

            networkMessage.Messages.Add(message);
            networkMessage.NetworkMessageContentMask = (uint)(
                JsonNetworkMessageContentMask.PublisherId |
                JsonNetworkMessageContentMask.NetworkMessageHeader |
                JsonNetworkMessageContentMask.SingleDataSetMessage |
                JsonNetworkMessageContentMask.DataSetClassId);

            var context = new ServiceMessageContext();
            var buffer = Assert.Single(networkMessage.Encode(context, 1024));

            ConvertToOpcUaUniversalTime(networkMessage);

            using (var stream = new MemoryStream(buffer)) {
                using (var decoder = new JsonDecoderEx(stream, context)) {
                    var result = decoder.ReadEncodeable(null, typeof(JsonNetworkMessage)) as JsonNetworkMessage;
                    Assert.Equal(networkMessage, result);
                }
            }
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
