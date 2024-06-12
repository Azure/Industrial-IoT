// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub
{
    using Azure.IIoT.OpcUa.Encoders.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Xunit;

    public class JsonNetworkMessageEncoderTests1
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MultipleMessagesNoNetworkMessageHeaderAndNoDataSetMessageHeaderRawEncoding(bool useArrayEnvelope)
        {
            var simple = CreateMessage(0x18, 0x62, 0x3f);
            simple.UseArrayEnvelope = useArrayEnvelope;
            var buffers = simple.Encode(new ServiceMessageContext(), 1024);
            var buffer = Assert.Single(buffers);
            Assert.Equal(new NewtonsoftJsonSerializer().Parse("""

[
  {
    "Temperature":25,
    "Pressure":1013,
    "Humidity":42
  }
]

"""), new NewtonsoftJsonSerializer().Parse(buffer));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MultipleMessagesNoNetworkMessageHeaderAndNoDataSetMessageHeaderVariantEncoding(bool useArrayEnvelope)
        {
            var simple = CreateMessage(0x18, 0x62, 0);
            simple.UseArrayEnvelope = useArrayEnvelope;
            var buffers = simple.Encode(new ServiceMessageContext(), 1024);
            var buffer = Assert.Single(buffers);

            // TODO: According to spec if no datavalue fields are selected,
            // the value is encoded as variant using reversible encoding
            // Even if the datatset mask flag is not set. This is not logical.
            // The pub sub formatter behaves differently, it uses raw encoding.
            Assert.Equal(new NewtonsoftJsonSerializer().Parse("""

[
  {
    "Temperature":{"Type":6,"Body":25},
    "Pressure":{"Type":6,"Body":1013},
    "Humidity":{"Type":6,"Body":42}
  }
]

"""), new NewtonsoftJsonSerializer().Parse(buffer));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MultipleMessagesNoNetworkMessageHeaderAndNoDataSetMessageHeader(bool useArrayEnvelope)
        {
            var simple = CreateMessage(0x18, 0x62, 0x1f);
            simple.UseArrayEnvelope = useArrayEnvelope;
            var buffers = simple.Encode(new ServiceMessageContext(), 1024);
            var buffer = Assert.Single(buffers);
            Assert.Equal(new NewtonsoftJsonSerializer().Parse("""

[
  {
    "Temperature":{"Value":25,"SourceTimestamp":"2021-09-27T18:45:19.555Z","ServerTimestamp":"2021-09-27T18:45:19.555Z"},
    "Pressure":{"Value":1013,"SourceTimestamp":"2021-09-27T18:45:19.555Z","ServerTimestamp":"2021-09-27T18:45:19.555Z"},
    "Humidity":{"Value":42,"StatusCode":{"Code":1073741824,"Symbol":"Uncertain"},"SourceTimestamp":"2021-09-27T18:45:19.555Z","ServerTimestamp":"2021-09-27T18:45:19.555Z"}
  }
]

"""), new NewtonsoftJsonSerializer().Parse(buffer));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MultipleMessagesNoNetworkMessageHeaderAndSubsetofDataSetMessageContent(bool useArrayEnvelope)
        {
            var simple = CreateMessage(0x1a, 0x62, 0x1f);
            simple.UseArrayEnvelope = useArrayEnvelope;
            var buffers = simple.Encode(new ServiceMessageContext(), 1024);
            var buffer = Assert.Single(buffers);
            Assert.Equal(new NewtonsoftJsonSerializer().Parse("""

[
  {
    "MetaDataVersion":{"MajorVersion":672338910,"MinorVersion":672341762},
    "MessageType":"ua-keyframe",
    "DataSetWriterName":"Writer100",
    "Payload":
    {
      "Temperature":{"Value":25,"SourceTimestamp":"2021-09-27T18:45:19.555Z","ServerTimestamp":"2021-09-27T18:45:19.555Z"},
      "Pressure":{"Value":1013,"SourceTimestamp":"2021-09-27T18:45:19.555Z","ServerTimestamp":"2021-09-27T18:45:19.555Z"},
      "Humidity":{"Value":42,"StatusCode":{"Code":1073741824,"Symbol":"Uncertain"},"SourceTimestamp":"2021-09-27T18:45:19.555Z","ServerTimestamp":"2021-09-27T18:45:19.555Z"}
    }
  }
]

"""), new NewtonsoftJsonSerializer().Parse(buffer));
        }

        [Fact]
        public void MultipleMessagesAllSwitchesSelectedAndReversableFieldEncoding()
        {
            var simple = CreateMessage(0x1b, 0xff, 0x1f);
            var buffers = simple.Encode(new ServiceMessageContext(), 1024);
            var buffer = Assert.Single(buffers);
            Assert.Equal(new NewtonsoftJsonSerializer().Parse("""

{
  "MessageId":"9279C0B3-DA88-45A4-AF74-451CEBF82DB0",
  "MessageType":"ua-data",
  "PublisherId":"MyPublisher",
  "DataSetClassId":"5ae1a63a-9757-4aa7-ab71-0d88931266fc",
  "Messages":
  [
    {
      "DataSetWriterId":100,
      "SequenceNumber":29766,
      "MetaDataVersion":{"MajorVersion":672338910,"MinorVersion":672341762},
      "Timestamp":"2021-09-27T18:45:19.555Z",
      "Status":1073741824,
      "MessageType":"ua-keyframe",
      "DataSetWriterName":"Writer100",
      "Payload":
      {
        "Temperature":{"Value":{"Type":6,"Body":25},"SourceTimestamp":"2021-09-27T18:45:19.555Z","ServerTimestamp":"2021-09-27T18:45:19.555Z"},
        "Pressure":{"Value":{"Type":6,"Body":1013},"SourceTimestamp":"2021-09-27T18:45:19.555Z","ServerTimestamp":"2021-09-27T18:45:19.555Z"},
        "Humidity":{"Value":{"Type":6,"Body":42},"StatusCode":1073741824,"SourceTimestamp":"2021-09-27T18:45:19.555Z","ServerTimestamp":"2021-09-27T18:45:19.555Z"}
      }
    }
  ]
}

"""), new NewtonsoftJsonSerializer().Parse(buffer));
        }

        [Fact]
        public void MultipleMessagesAllSwitchesSelectedAndReversableFieldEncodingWithArrayEnvelope()
        {
            var simple = CreateMessage(0x1b, 0xff, 0x1f);
            simple.UseArrayEnvelope = true;
            var buffers = simple.Encode(new ServiceMessageContext(), 1024);
            var buffer = Assert.Single(buffers);
            Assert.Equal(new NewtonsoftJsonSerializer().Parse("""

[
  {
    "MessageId":"9279C0B3-DA88-45A4-AF74-451CEBF82DB0",
    "MessageType":"ua-data",
    "PublisherId":"MyPublisher",
    "DataSetClassId":"5ae1a63a-9757-4aa7-ab71-0d88931266fc",
    "Messages":
    [
      {
        "DataSetWriterId":100,
        "SequenceNumber":29766,
        "MetaDataVersion":{"MajorVersion":672338910,"MinorVersion":672341762},
        "Timestamp":"2021-09-27T18:45:19.555Z",
        "Status":1073741824,
        "MessageType":"ua-keyframe",
        "DataSetWriterName":"Writer100",
        "Payload":
        {
          "Temperature":{"Value":{"Type":6,"Body":25},"SourceTimestamp":"2021-09-27T18:45:19.555Z","ServerTimestamp":"2021-09-27T18:45:19.555Z"},
          "Pressure":{"Value":{"Type":6,"Body":1013},"SourceTimestamp":"2021-09-27T18:45:19.555Z","ServerTimestamp":"2021-09-27T18:45:19.555Z"},
          "Humidity":{"Value":{"Type":6,"Body":42},"StatusCode":1073741824,"SourceTimestamp":"2021-09-27T18:45:19.555Z","ServerTimestamp":"2021-09-27T18:45:19.555Z"}
        }
      }
    ]
  }
]

"""), new NewtonsoftJsonSerializer().Parse(buffer));
        }

        [Fact]
        public void MultipleMessagesAllSwitchesSelectedNoReversableFieldEncoding()
        {
            var simple = CreateMessage(0x1b, 0x7f, 0x1f);
            var buffers = simple.Encode(new ServiceMessageContext(), 1024);
            var buffer = Assert.Single(buffers);
            Assert.Equal(new NewtonsoftJsonSerializer().Parse("""

{
  "MessageId":"9279C0B3-DA88-45A4-AF74-451CEBF82DB0",
  "MessageType":"ua-data",
  "PublisherId":"MyPublisher",
  "DataSetClassId":"5ae1a63a-9757-4aa7-ab71-0d88931266fc",
  "Messages":
  [
    {
      "DataSetWriterId":100,
      "SequenceNumber":29766,
      "MetaDataVersion":{"MajorVersion":672338910,"MinorVersion":672341762},
      "Timestamp":"2021-09-27T18:45:19.555Z",
      "Status":1073741824,
      "MessageType":"ua-keyframe",
      "DataSetWriterName":"Writer100",
      "Payload":
      {
        "Temperature":{"Value":25,"SourceTimestamp":"2021-09-27T18:45:19.555Z","ServerTimestamp":"2021-09-27T18:45:19.555Z"},
        "Pressure":{"Value":1013,"SourceTimestamp":"2021-09-27T18:45:19.555Z","ServerTimestamp":"2021-09-27T18:45:19.555Z"},
        "Humidity":{"Value":42,"StatusCode":{"Code":1073741824,"Symbol":"Uncertain"},"SourceTimestamp":"2021-09-27T18:45:19.555Z","ServerTimestamp":"2021-09-27T18:45:19.555Z"}
      }
    }
  ]
}

"""), new NewtonsoftJsonSerializer().Parse(buffer));
        }

        [Fact]
        public void SingleMessageNoNetworkMessageHeaderAndNoDataSetMessageHeader()
        {
            var simple = CreateMessage(0x1c, 0x62, 0x1f);
            var buffers = simple.Encode(new ServiceMessageContext(), 1024);
            var buffer = Assert.Single(buffers);
            Assert.Equal(new NewtonsoftJsonSerializer().Parse("""

{
  "Temperature":{"Value":25,"SourceTimestamp":"2021-09-27T18:45:19.555Z","ServerTimestamp":"2021-09-27T18:45:19.555Z"},
  "Pressure":{"Value":1013,"SourceTimestamp":"2021-09-27T18:45:19.555Z","ServerTimestamp":"2021-09-27T18:45:19.555Z"},
  "Humidity":{"Value":42,"StatusCode":{"Code":1073741824,"Symbol":"Uncertain"},"SourceTimestamp":"2021-09-27T18:45:19.555Z","ServerTimestamp":"2021-09-27T18:45:19.555Z"}
}

"""), new NewtonsoftJsonSerializer().Parse(buffer));
        }

        [Fact]
        public void SingleMessageNoNetworkMessageHeaderAndSubsetofDataSetMessageContent()
        {
            var simple = CreateMessage(0x1e, 0x62, 0x1f);
            var buffers = simple.Encode(new ServiceMessageContext(), 1024);
            var buffer = Assert.Single(buffers);
            Assert.Equal(new NewtonsoftJsonSerializer().Parse("""

{
  "MetaDataVersion":{"MajorVersion":672338910,"MinorVersion":672341762},
  "MessageType":"ua-keyframe",
  "DataSetWriterName":"Writer100",
  "Payload":
  {
    "Temperature":{"Value":25,"SourceTimestamp":"2021-09-27T18:45:19.555Z","ServerTimestamp":"2021-09-27T18:45:19.555Z"},
    "Pressure":{"Value":1013,"SourceTimestamp":"2021-09-27T18:45:19.555Z","ServerTimestamp":"2021-09-27T18:45:19.555Z"},
    "Humidity":{"Value":42,"StatusCode":{"Code":1073741824,"Symbol":"Uncertain"},"SourceTimestamp":"2021-09-27T18:45:19.555Z","ServerTimestamp":"2021-09-27T18:45:19.555Z"}
  }
}

"""), new NewtonsoftJsonSerializer().Parse(buffer));
        }

        [Fact]
        public void SingleMessageAllSwitchesSelectedAndReversableFieldEncoding()
        {
            var simple = CreateMessage(0x1f, 0xff, 0x1f);
            var buffers = simple.Encode(new ServiceMessageContext(), 1024);
            var buffer = Assert.Single(buffers);
            Assert.Equal(new NewtonsoftJsonSerializer().Parse("""

{
  "MessageId":"9279C0B3-DA88-45A4-AF74-451CEBF82DB0",
  "MessageType":"ua-data",
  "PublisherId":"MyPublisher",
  "DataSetClassId":"5ae1a63a-9757-4aa7-ab71-0d88931266fc",
  "Messages":
  {
    "DataSetWriterId":100,
    "SequenceNumber":29766,
    "MetaDataVersion":{"MajorVersion":672338910,"MinorVersion":672341762},
    "Timestamp":"2021-09-27T18:45:19.555Z",
    "Status":1073741824,
    "MessageType":"ua-keyframe",
    "DataSetWriterName":"Writer100",
    "Payload":
    {
      "Temperature":{"Value":{"Type":6,"Body":25},"SourceTimestamp":"2021-09-27T18:45:19.555Z","ServerTimestamp":"2021-09-27T18:45:19.555Z"},
      "Pressure":{"Value":{"Type":6,"Body":1013},"SourceTimestamp":"2021-09-27T18:45:19.555Z","ServerTimestamp":"2021-09-27T18:45:19.555Z"},
      "Humidity":{"Value":{"Type":6,"Body":42},"StatusCode":1073741824,"SourceTimestamp":"2021-09-27T18:45:19.555Z","ServerTimestamp":"2021-09-27T18:45:19.555Z"}
    }
  }
}

"""), new NewtonsoftJsonSerializer().Parse(buffer));
        }

        [Fact]
        public void SingleMessageAllSwitchesSelectedNoReversableFieldEncoding()
        {
            var simple = CreateMessage(0x1f, 0x7f, 0x1f);
            var buffers = simple.Encode(new ServiceMessageContext(), 1024);
            var buffer = Assert.Single(buffers);
            Assert.Equal(new NewtonsoftJsonSerializer().Parse("""

{
  "MessageId":"9279C0B3-DA88-45A4-AF74-451CEBF82DB0",
  "MessageType":"ua-data",
  "PublisherId":"MyPublisher",
  "DataSetClassId":"5ae1a63a-9757-4aa7-ab71-0d88931266fc",
  "Messages":
  {
    "DataSetWriterId":100,
    "SequenceNumber":29766,
    "MetaDataVersion":{"MajorVersion":672338910,"MinorVersion":672341762},
    "Timestamp":"2021-09-27T18:45:19.555Z",
    "Status":1073741824,
    "MessageType":"ua-keyframe",
    "DataSetWriterName":"Writer100",
    "Payload":
    {
      "Temperature":{"Value":25,"SourceTimestamp":"2021-09-27T18:45:19.555Z","ServerTimestamp":"2021-09-27T18:45:19.555Z"},
      "Pressure":{"Value":1013,"SourceTimestamp":"2021-09-27T18:45:19.555Z","ServerTimestamp":"2021-09-27T18:45:19.555Z"},
      "Humidity":{"Value":42,"StatusCode":{"Code":1073741824,"Symbol":"Uncertain"},"SourceTimestamp":"2021-09-27T18:45:19.555Z","ServerTimestamp":"2021-09-27T18:45:19.555Z"}
    }
  }
}

"""), new NewtonsoftJsonSerializer().Parse(buffer));
        }

        [Fact]
        public void SingleMessageAllSwitchesSelectedRawValueEncoding()
        {
            var simple = CreateMessage(0x1f, 0x7f, 0x3f);
            var buffers = simple.Encode(new ServiceMessageContext(), 1024);
            var buffer = Assert.Single(buffers);
            Assert.Equal(new NewtonsoftJsonSerializer().Parse("""

{
  "MessageId":"9279C0B3-DA88-45A4-AF74-451CEBF82DB0",
  "MessageType":"ua-data",
  "PublisherId":"MyPublisher",
  "DataSetClassId":"5ae1a63a-9757-4aa7-ab71-0d88931266fc",
  "Messages":
  {
    "DataSetWriterId":100,
    "SequenceNumber":29766,
    "MetaDataVersion":{"MajorVersion":672338910,"MinorVersion":672341762},
    "Timestamp":"2021-09-27T18:45:19.555Z",
    "Status":1073741824,
    "MessageType":"ua-keyframe",
    "DataSetWriterName":"Writer100",
    "Payload":
    {
      "Temperature":25,
      "Pressure":1013,
      "Humidity":42
    }
  }
}

"""), new NewtonsoftJsonSerializer().Parse(buffer));
        }

        [Fact]
        public void SingleMessageAllSwitchesSelectedJustStatusFieldAndArrayEnvelope()
        {
            var simple = CreateMessage(0x1f, 0x7f, 0x1);
            simple.UseArrayEnvelope = true;
            var buffers = simple.Encode(new ServiceMessageContext(), 1024);
            var buffer = Assert.Single(buffers);
            Assert.Equal(new NewtonsoftJsonSerializer().Parse("""

[
  {
    "MessageId":"9279C0B3-DA88-45A4-AF74-451CEBF82DB0",
    "MessageType":"ua-data",
    "PublisherId":"MyPublisher",
    "DataSetClassId":"5ae1a63a-9757-4aa7-ab71-0d88931266fc",
    "Messages":
    {
      "DataSetWriterId":100,
      "SequenceNumber":29766,
      "MetaDataVersion":{"MajorVersion":672338910,"MinorVersion":672341762},
      "Timestamp":"2021-09-27T18:45:19.555Z",
      "Status":1073741824,
      "MessageType":"ua-keyframe",
      "DataSetWriterName":"Writer100",
      "Payload":
      {
        "Temperature":{"Value":25},
        "Pressure":{"Value":1013},
        "Humidity":{"Value":42,"StatusCode":{"Code":1073741824,"Symbol":"Uncertain"}}
      }
    }
  }
]

"""), new NewtonsoftJsonSerializer().Parse(buffer));
        }

        [Fact]
        public void SingleMessageAllSwitchesSelectedNoReversableFieldEncodingWithArrayEnvelope()
        {
            var simple = CreateMessage(0x1f, 0x7f, 0x1f);
            simple.UseArrayEnvelope = true;
            var buffers = simple.Encode(new ServiceMessageContext(), 1024);
            var buffer = Assert.Single(buffers);
            Assert.Equal(new NewtonsoftJsonSerializer().Parse("""

[
  {
    "MessageId":"9279C0B3-DA88-45A4-AF74-451CEBF82DB0",
    "MessageType":"ua-data",
    "PublisherId":"MyPublisher",
    "DataSetClassId":"5ae1a63a-9757-4aa7-ab71-0d88931266fc",
    "Messages":
    {
      "DataSetWriterId":100,
      "SequenceNumber":29766,
      "MetaDataVersion":{"MajorVersion":672338910,"MinorVersion":672341762},
      "Timestamp":"2021-09-27T18:45:19.555Z",
      "Status":1073741824,
      "MessageType":"ua-keyframe",
      "DataSetWriterName":"Writer100",
      "Payload":
      {
        "Temperature":{"Value":25,"SourceTimestamp":"2021-09-27T18:45:19.555Z","ServerTimestamp":"2021-09-27T18:45:19.555Z"},
        "Pressure":{"Value":1013,"SourceTimestamp":"2021-09-27T18:45:19.555Z","ServerTimestamp":"2021-09-27T18:45:19.555Z"},
        "Humidity":{"Value":42,"StatusCode":{"Code":1073741824,"Symbol":"Uncertain"},"SourceTimestamp":"2021-09-27T18:45:19.555Z","ServerTimestamp":"2021-09-27T18:45:19.555Z"}
      }
    }
  }
]
"""), new NewtonsoftJsonSerializer().Parse(buffer));
        }

        private static JsonNetworkMessage CreateMessage(uint messageMask, uint datasetMask, uint fieldMask)
        {
            return new JsonNetworkMessage
            {
                DataSetClassId = Guid.Parse("5ae1a63a-9757-4aa7-ab71-0d88931266fc"),
                NetworkMessageContentMask = PubSubMessageContentFlagHelper.StackToNetworkMessageContentFlags(messageMask),
                MessageId = () => "9279C0B3-DA88-45A4-AF74-451CEBF82DB0",
                PublisherId = "MyPublisher",
                Messages = new List<BaseDataSetMessage> {
                    new JsonDataSetMessage {
                        DataSetMessageContentMask = PubSubMessageContentFlagHelper.StackToDataSetMessageContentFlags(datasetMask),
                        DataSetWriterId = 100,
                        SequenceNumber = 29766,
                        MetaDataVersion = new ConfigurationVersionDataType
                        {
                            MajorVersion = 672338910, MinorVersion = 672341762
                        },
                        Timestamp = DateTime.Parse("2021-09-27T18:45:19.555Z", CultureInfo.InvariantCulture),
                        Status = 1073741824,
                        MessageType = MessageType.KeyFrame,
                        DataSetWriterName = "Writer100",
                        Payload = new DataSet(PubSubMessageContentFlagHelper.StackToDataSetFieldContentFlags(fieldMask)) {
    ["Temperature"] = new DataValue(25, StatusCodes.Good,
        DateTime.Parse("2021-09-27T18:45:19.555Z", CultureInfo.InvariantCulture),
        DateTime.Parse("2021-09-27T18:45:19.555Z", CultureInfo.InvariantCulture)),
    ["Pressure"] = new DataValue(1013, StatusCodes.Good,
        DateTime.Parse("2021-09-27T18:45:19.555Z", CultureInfo.InvariantCulture),
        DateTime.Parse("2021-09-27T18:45:19.555Z", CultureInfo.InvariantCulture)),
    ["Humidiy"] = new DataValue(42, StatusCodes.Uncertain,
        DateTime.Parse("2021-09-27T18:45:19.555Z", CultureInfo.InvariantCulture),
        DateTime.Parse("2021-09-27T18:45:19.555Z", CultureInfo.InvariantCulture))
                        }
                    }
                }
            };
        }
    }
}
