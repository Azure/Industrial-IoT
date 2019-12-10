

namespace Opc.Ua.PubSub.Tests {

    using System;
    using System.IO;
    using Opc.Ua;
    using System.Collections.Generic;    
    using Opc.Ua.Encoders;
    using Opc.Ua.PubSub;
    using Xunit;

    public class PubSubJsonEncoderDecoder {
        
        private uint currentSequenceNumber = 0;

        [Fact]
        public void EncodeDecodeNetworkMessage() {
            var payload = new Dictionary<string, DataValue>();

            payload.Add("1", new DataValue(new Variant(5), StatusCodes.Good, DateTime.Now));
            payload.Add("2", new DataValue(new Variant(0.5), StatusCodes.Good, DateTime.Now));
            payload.Add("3", new DataValue("abcd"));

            var message = new DataSetMessage();
            message.DataSetWriterId = "WriterId";
            message.MetaDataVersion = new ConfigurationVersionDataType { MajorVersion = 1, MinorVersion = 1 };
            message.SequenceNumber = ++currentSequenceNumber;
            message.Status = StatusCodes.Good;
            message.Timestamp = DateTime.UtcNow;
            message.MessageContentMask = (uint)(
                JsonDataSetMessageContentMask.DataSetWriterId |
                JsonDataSetMessageContentMask.SequenceNumber |
                JsonDataSetMessageContentMask.MetaDataVersion |
                JsonDataSetMessageContentMask.Timestamp |
                JsonDataSetMessageContentMask.Status);
            message.Payload = new DataSet(payload);
            message.Payload.FieldContentMask = (uint)(DataSetFieldContentMask.StatusCode | DataSetFieldContentMask.SourceTimestamp);

            var networkMessage = new NetworkMessage();
            networkMessage.MessageId = Guid.NewGuid().ToString(); // TODO
            networkMessage.MessageType = "ua-data";
            networkMessage.Messages = new List<DataSetMessage>();
            networkMessage.PublisherId = "PublisherId";

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
