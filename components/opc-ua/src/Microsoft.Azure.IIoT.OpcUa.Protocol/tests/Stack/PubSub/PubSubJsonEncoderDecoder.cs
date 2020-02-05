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

    public class PubSubJsonEncoderDecoder {

        private uint _currentSequenceNumber = 0;

        [Fact]
        public void EncodeDecodeNetworkMessage() {
            var payload = new Dictionary<string, DataValue> {
                { "1", new DataValue(new Variant(5), StatusCodes.Good, DateTime.Now) },
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
                Payload = new DataSet(payload) {
                    FieldContentMask = (uint)(DataSetFieldContentMask.StatusCode | DataSetFieldContentMask.SourceTimestamp)
                }
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
            string json;
            var context = new ServiceMessageContext();
            using (var stream = new MemoryStream()) {
                
                using (var encoder = new JsonEncoderEx(stream, context)) {
                    networkMessage.Encode(encoder);
                }
                buffer = stream.ToArray();
                json = buffer.ToBase16String();
            }

            using (var stream = new MemoryStream(buffer)) {
                using (var decoder = new JsonDecoderEx(stream, context)) {
                    var result = decoder.ReadEncodeable(null, typeof(NetworkMessage)) as NetworkMessage;
                    Assert.Equal(networkMessage, result);
                }
            }
        }
    }
}
