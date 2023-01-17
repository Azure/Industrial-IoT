# Telemetry Messages Processing

[Home](../../readme.md)

## Messaging Profiles supported by OPC Publisher

| Messaging Mode<br>(--mm) | Message Encoding<br>(--me) | NetworkMessageContentMask | DataSetMessageContentMask | DataSetFieldContentMask | Metadata supported | KeyFrames supported |
   |--------------------------|----------------------------|---------------------------|---------------------------|-------------------------|--------------------|---------------------|
| Samples | Json | PublisherId, WriterGroupId, NetworkMessageNumber, SequenceNumber, PayloadHeader, Timestamp, DataSetClassId, DataSetMessageHeader, MonitoredItemMessage<br>(0x2) | MetaDataVersion, MajorVersion, MinorVersion, MessageType, DataSetWriterName<br>(0xB0000062) | StatusCode, SourceTimestamp, NodeId, DisplayName, EndpointUrl<br>(0x3) |   |   |
| FullSamples | Json | PublisherId, WriterGroupId, NetworkMessageNumber, SequenceNumber, PayloadHeader, Timestamp, DataSetClassId, DataSetMessageHeader, MonitoredItemMessage<br>(0x2) | Timestamp, MetaDataVersion, DataSetWriterId, MajorVersion, MinorVersion, SequenceNumber, MessageType, DataSetWriterName<br>(0xF200006F) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) |   |   |
| PubSub | Json | PublisherId, WriterGroupId, NetworkMessageNumber, SequenceNumber, PayloadHeader, Timestamp, DataSetClassId, NetworkMessageHeader, DataSetMessageHeader<br>(0x1B) | MetaDataVersion, MajorVersion, MinorVersion, MessageType, DataSetWriterName<br>(0xB0000062) | StatusCode, SourceTimestamp, NodeId, DisplayName, EndpointUrl<br>(0x3) | X | X |
| FullNetworkMessages | Json | PublisherId, WriterGroupId, NetworkMessageNumber, SequenceNumber, PayloadHeader, Timestamp, DataSetClassId, NetworkMessageHeader, DataSetMessageHeader<br>(0x1B) | Timestamp, MetaDataVersion, DataSetWriterId, MajorVersion, MinorVersion, SequenceNumber, MessageType, DataSetWriterName<br>(0xF200006F) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) | X | X |
| PubSub | JsonGzip | PublisherId, WriterGroupId, NetworkMessageNumber, SequenceNumber, PayloadHeader, Timestamp, DataSetClassId, NetworkMessageHeader, DataSetMessageHeader<br>(0x1B) | MetaDataVersion, MajorVersion, MinorVersion, MessageType, DataSetWriterName<br>(0xB0000062) | StatusCode, SourceTimestamp, NodeId, DisplayName, EndpointUrl<br>(0x3) | X | X |
| FullNetworkMessages | JsonGzip | PublisherId, WriterGroupId, NetworkMessageNumber, SequenceNumber, PayloadHeader, Timestamp, DataSetClassId, NetworkMessageHeader, DataSetMessageHeader<br>(0x1B) | Timestamp, MetaDataVersion, DataSetWriterId, MajorVersion, MinorVersion, SequenceNumber, MessageType, DataSetWriterName<br>(0xF200006F) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) | X | X |
| PubSub | JsonReversible | PublisherId, WriterGroupId, NetworkMessageNumber, SequenceNumber, PayloadHeader, Timestamp, DataSetClassId, NetworkMessageHeader, DataSetMessageHeader<br>(0x1B) | MetaDataVersion, MajorVersion, MinorVersion, MessageType, DataSetWriterName, ReversibleFieldEncoding<br>(0xB00000E2) | StatusCode, SourceTimestamp, NodeId, DisplayName, EndpointUrl<br>(0x3) | X | X |
| PubSub | JsonReversibleGzip | PublisherId, WriterGroupId, NetworkMessageNumber, SequenceNumber, PayloadHeader, Timestamp, DataSetClassId, NetworkMessageHeader, DataSetMessageHeader<br>(0x1B) | MetaDataVersion, MajorVersion, MinorVersion, MessageType, DataSetWriterName, ReversibleFieldEncoding<br>(0xB00000E2) | StatusCode, SourceTimestamp, NodeId, DisplayName, EndpointUrl<br>(0x3) | X | X |
| FullNetworkMessages | JsonReversible | PublisherId, WriterGroupId, NetworkMessageNumber, SequenceNumber, PayloadHeader, Timestamp, DataSetClassId, NetworkMessageHeader, DataSetMessageHeader<br>(0x1B) | Timestamp, MetaDataVersion, DataSetWriterId, MajorVersion, MinorVersion, SequenceNumber, MessageType, DataSetWriterName, ReversibleFieldEncoding<br>(0xF20000EF) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) | X | X |
| FullNetworkMessages | JsonReversibleGzip | PublisherId, WriterGroupId, NetworkMessageNumber, SequenceNumber, PayloadHeader, Timestamp, DataSetClassId, NetworkMessageHeader, DataSetMessageHeader<br>(0x1B) | Timestamp, MetaDataVersion, DataSetWriterId, MajorVersion, MinorVersion, SequenceNumber, MessageType, DataSetWriterName, ReversibleFieldEncoding<br>(0xF20000EF) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) | X | X |
| Samples | JsonReversible | PublisherId, WriterGroupId, NetworkMessageNumber, SequenceNumber, PayloadHeader, Timestamp, DataSetClassId, DataSetMessageHeader, MonitoredItemMessage<br>(0x2) | MetaDataVersion, MajorVersion, MinorVersion, MessageType, DataSetWriterName, ReversibleFieldEncoding<br>(0xB00000E2) | StatusCode, SourceTimestamp, NodeId, DisplayName, EndpointUrl<br>(0x3) |   |   |
| Samples | JsonReversibleGzip | PublisherId, WriterGroupId, NetworkMessageNumber, SequenceNumber, PayloadHeader, Timestamp, DataSetClassId, DataSetMessageHeader, MonitoredItemMessage<br>(0x2) | MetaDataVersion, MajorVersion, MinorVersion, MessageType, DataSetWriterName, ReversibleFieldEncoding<br>(0xB00000E2) | StatusCode, SourceTimestamp, NodeId, DisplayName, EndpointUrl<br>(0x3) |   |   |
| FullSamples | JsonReversible | PublisherId, WriterGroupId, NetworkMessageNumber, SequenceNumber, PayloadHeader, Timestamp, DataSetClassId, DataSetMessageHeader, MonitoredItemMessage<br>(0x2) | Timestamp, MetaDataVersion, DataSetWriterId, MajorVersion, MinorVersion, SequenceNumber, MessageType, DataSetWriterName, ReversibleFieldEncoding<br>(0xF20000EF) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) |   |   |
| FullSamples | JsonReversibleGzip | PublisherId, WriterGroupId, NetworkMessageNumber, SequenceNumber, PayloadHeader, Timestamp, DataSetClassId, DataSetMessageHeader, MonitoredItemMessage<br>(0x2) | Timestamp, MetaDataVersion, DataSetWriterId, MajorVersion, MinorVersion, SequenceNumber, MessageType, DataSetWriterName, ReversibleFieldEncoding<br>(0xF20000EF) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) |   |   |
| DataSetMessages | Json | DataSetMessageHeader<br>(0x2) | Timestamp, MetaDataVersion, DataSetWriterId, MajorVersion, MinorVersion, SequenceNumber, MessageType, DataSetWriterName<br>(0xF200006F) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) | X | X |
| DataSetMessages | JsonGzip | DataSetMessageHeader<br>(0x2) | Timestamp, MetaDataVersion, DataSetWriterId, MajorVersion, MinorVersion, SequenceNumber, MessageType, DataSetWriterName<br>(0xF200006F) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) | X | X |
| DataSetMessages | JsonReversible | DataSetMessageHeader<br>(0x2) | Timestamp, MetaDataVersion, DataSetWriterId, MajorVersion, MinorVersion, SequenceNumber, MessageType, DataSetWriterName, ReversibleFieldEncoding<br>(0xF20000EF) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) | X | X |
| DataSetMessages | JsonReversibleGzip | DataSetMessageHeader<br>(0x2) | Timestamp, MetaDataVersion, DataSetWriterId, MajorVersion, MinorVersion, SequenceNumber, MessageType, DataSetWriterName, ReversibleFieldEncoding<br>(0xF20000EF) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) | X | X |
| RawDataSets | Json | 0<br>(0x0) | 0<br>(0x0) | RawData<br>(0x20) |   | X |
| RawDataSets | JsonGzip | 0<br>(0x0) | 0<br>(0x0) | RawData<br>(0x20) |   | X |
| PubSub | Uadp | PublisherId, WriterGroupId, NetworkMessageNumber, SequenceNumber, PayloadHeader, Timestamp, DataSetClassId, NetworkMessageHeader, DataSetMessageHeader<br>(0x2F5) | MetaDataVersion, MajorVersion, MinorVersion, MessageType, DataSetWriterName<br>(0x18) | StatusCode, SourceTimestamp, NodeId, DisplayName, EndpointUrl<br>(0x3) | X | X |
| FullNetworkMessages | Uadp | PublisherId, WriterGroupId, NetworkMessageNumber, SequenceNumber, PayloadHeader, Timestamp, DataSetClassId, NetworkMessageHeader, DataSetMessageHeader<br>(0x2F5) | Timestamp, MetaDataVersion, DataSetWriterId, MajorVersion, MinorVersion, SequenceNumber, MessageType, DataSetWriterName<br>(0x39) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) | X | X |
| DataSetMessages | Uadp | DataSetMessageHeader<br>(0x0) | Timestamp, MetaDataVersion, DataSetWriterId, MajorVersion, MinorVersion, SequenceNumber, MessageType, DataSetWriterName, ReversibleFieldEncoding<br>(0x39) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) | X | X |
| RawDataSets | Uadp | 0<br>(0x0) | 0<br>(0x0) | RawData<br>(0x20) |   | X |

## Data value changes arriving at IoT Hub from OPC Publisher in standalone mode

Telemetry messages used by the IIoT Platform.  Please use PubSub format in all new projects.  To use the OPC UA PubSub format specify the `--mm=PubSub` command line. This needs to be done because the OPC publisher defaults to `--mm=Samples` mode which existed before the introduction of OPC UA standards compliant PubSub format.You should always use PubSub format specified in the OPC UA Standard. We might decide to not support the non standards compliant Samples mode in future versions of OPC Publisher.

Details for OPC UA Alarms and Events messages can be found [in this seperate document](./telemetry-events-format.md).

The following messages are emitted for data value changes in a subscription if PubSub mode is used:

```json
{
  "body": {
    "MessageId": "27",
    "MessageType": "ua-data",
    "PublisherId": "opc.tcp://opcplc:50000_70FB9F43",
    "Messages": [
      {
        "DataSetWriterId": 1,
        "DataSetWriterName": "1000",
        "SequenceNumber": 27,
        "MetaDataVersion": {
          "MajorVersion": 1,
          "MinorVersion": 0
        },
        "MessageType": "ua-deltaframe",
        "Timestamp": "2022-03-18T12:55:21.3424136Z",
        "Payload": {
          "AlternatingBoolean": {
            "Value": true,
            "SourceTimestamp": "2022-03-18T12:55:20.9313098Z",
            "ServerTimestamp": "2022-03-18T12:55:20.9314784Z"
          },
          "StepUp": {
            "Value": 23305,
            "SourceTimestamp": "2022-03-18T12:55:21.3313539Z",
            "ServerTimestamp": "2022-03-18T12:55:21.3313638Z"
          },
          "RandomSignedInt32": {
            "Value": 1076635612,
            "SourceTimestamp": "2022-03-18T12:55:21.3419164Z",
            "ServerTimestamp": "2022-03-18T12:55:21.3419728Z"
          },
          "RandomUnsignedInt32": {
            "Value": 1461169798,
            "SourceTimestamp": "2022-03-18T12:55:21.3419727Z",
            "ServerTimestamp": "2022-03-18T12:55:21.3420045Z"
          },
          "BadFastUInt1": {
            "StatusCode": {
              "Symbol": "BadNoCommunication",
              "Code": 2150694912
            },
            "SourceTimestamp": "2022-03-18T12:55:20.8409353Z",
            "ServerTimestamp": "2022-03-18T12:55:20.8409362Z"
          }
        }
      }
    ]
  },
  "enqueuedTime": "Fri Mar 18 2022 13:55:21 GMT+0100 (Central European Standard Time)",
  "properties": {
    "$$ContentType": "application/x-network-message-json-v1",
    "iothub-message-schema": "application/json",
    "$$ContentEncoding": "utf-8"
  }
}
```

The data set messages in the `ua-data` network message can be delta frames (`ua-deltaframe`, containing only changed values in the dataset), key frames (`ua-keyframe`, containing all values of the dataset), keep alives (`ua-keepalive`, containing no payload), or [events and conditions](./telemetry-events-format.md).

The data set is described by the corresponding metadata message (message type `ua-metdata`), which is emitted prior to the first message and whenever the configuration is updated requiring an update of the metadata. Metadata can also be sent periodically, which can be configured using the control plane of OPC Publisher.

```json
{
  "body": [
    {
      "MessageId": "0",
      "MessageType": "ua-metadata",
      "PublisherId": "opc.tcp://localhost:57537/UA/SampleServer_A2425855",
      "DataSetWriterId": 1,
      "MetaData": {
        "Namespaces": [
          "http://opcfoundation.org/UA/",
          "urn:localhost:OPCFoundation:CoreSampleServer",
          "http://test.org/UA/Data/",
          "http://opcfoundation.org/UA/Boiler/"
        ],
        "StructureDataTypes": [],
        "EnumDataTypes": [],
        "SimpleDataTypes": [],
        "Fields": [
          {
            "Name": "Output",
            "BuiltInType": 26,
            "DataType": "Number",
            "ValueRank": -1,
            "ArrayDimensions": [],
            "DataSetFieldId": "fcab2ed0-c6b2-4456-a4c3-ed985e5c708d",
            "Properties": []
          }
        ],
        "ConfigurationVersion": {
          "MajorVersion": 1222304635,
          "MinorVersion": 1289056823
        }
      }
    }
  ],
  "enqueuedTime": "Mon Jan 23 2023 13:49:02 GMT+0200 (Central European Summer Time)",
  "properties": {
    "$$ContentType": "application/x-network-message-json-v1",
    "iothub-message-schema": "application/ua+json",
    "$$ContentEncoding": "utf-8"
  }
}

```

### Samples mode

In samples mode the messages will look like this:

```json
{
  "body": {
    "NodeId": "nsu=http://microsoft.com/Opc/OpcPlc/;s=StepUp",
    "EndpointUrl": "opc.tcp://opcplc:50000/",
    "ApplicationUri": "urn:OpcPlc:opcplc",
    "DisplayName": "StepUp",
    "Timestamp": "2022-03-18T12:52:42.137703Z",
    "Value": {
      "Value": 21713,
      "SourceTimestamp": "2022-03-18T12:52:42.1327544Z",
      "ServerTimestamp": "2022-03-18T12:52:42.1327633Z"
    },
    "SequenceNumber": 120,
    "ExtensionFields": {
      "PublisherId": "opc.tcp://opcplc:50000_D09D61EF",
      "DataSetWriterId": "1000"
    }
  },
  "enqueuedTime": "Fri Mar 18 2022 13:52:42 GMT+0100 (Central European Standard Time)",
  "properties": {
    "$$ContentType": "application/x-monitored-item-json-v1",
    "iothub-message-schema": "application/json",
    "$$ContentEncoding": "utf-8"
  }
}
```

To provide compatibility with new version of the IIoT Platform's telemetry processors the OPC Publisher should be started in standalone mode with `--fm=true` argument and produces messages like shown here:

```json
{
  "body": {
    "NodeId": "nsu=http://microsoft.com/Opc/OpcPlc/;s=RandomUnsignedInt32",
    "EndpointUrl": "opc.tcp://opcplc:50000/",
    "ApplicationUri": "urn:OpcPlc:opcplc",
    "Timestamp": "2022-03-18T12:58:45.6660994Z",
    "Value": {
      "Value": 1059185306,
      "SourceTimestamp": "2022-03-18T12:58:45.6329923Z",
      "ServerTimestamp": "2022-03-18T12:58:45.6331823Z"
    },
    "SequenceNumber": 22,
    "ExtensionFields": {
      "PublisherId": "opc.tcp://opcplc:50000_D3C751BF",
      "DataSetWriterId": "1000"
    }
  },
  "enqueuedTime": "Fri Mar 18 2022 13:58:45 GMT+0100 (Central European Standard Time)",
  "properties": {
    "$$ContentType": "application/x-monitored-item-json-v1",
    "iothub-message-schema": "application/json",
    "$$ContentEncoding": "utf-8"
  }
}

{
  "body": {
    "NodeId": "nsu=http://microsoft.com/Opc/OpcPlc/;s=BadFastUInt1",
    "EndpointUrl": "opc.tcp://opcplc:50000/",
    "ApplicationUri": "urn:OpcPlc:opcplc",
    "Timestamp": "2022-03-18T12:58:41.6538735Z",
    "Value": {
      "StatusCode": {
        "Symbol": "BadNoCommunication",
        "Code": 2150694912
      },
      "SourceTimestamp": "2022-03-18T12:58:40.840659Z",
      "ServerTimestamp": "2022-03-18T12:58:40.8406599Z"
    },
    "SequenceNumber": 18,
    "ExtensionFields": {
      "PublisherId": "opc.tcp://opcplc:50000_D3C751BF",
      "DataSetWriterId": "1000"
    }
  },
  "enqueuedTime": "Fri Mar 18 2022 13:58:41 GMT+0100 (Central European Standard Time)",
  "properties": {
    "$$ContentType": "application/x-monitored-item-json-v1",
    "iothub-message-schema": "application/json",
    "$$ContentEncoding": "utf-8"
  }
}
```

The following message is an example with batching/bulk mode enabled. Here OPC Publisher was started in standalone mode with `--bs=5` argument, where 5 is the number of value-change messages to be batched.

```json

  "body": [
    {
      "NodeId": "nsu=http://microsoft.com/Opc/OpcPlc/;s=AlternatingBoolean",
      "EndpointUrl": "opc.tcp://opcplc:50000/",
      "ApplicationUri": "urn:OpcPlc:opcplc",
      "Timestamp": "2022-03-18T13:01:56.7551553Z",
      "Value": {
        "Value": false,
        "SourceTimestamp": "2022-03-18T13:01:55.9333398Z",
        "ServerTimestamp": "2022-03-18T13:01:55.933447Z"
      },
      "SequenceNumber": 22,
      "ExtensionFields": {
        "PublisherId": "opc.tcp://opcplc:50000_9C43F84E",
        "DataSetWriterId": "1000"
      }
    },
    {
      "NodeId": "nsu=http://microsoft.com/Opc/OpcPlc/;s=StepUp",
      "EndpointUrl": "opc.tcp://opcplc:50000/",
      "ApplicationUri": "urn:OpcPlc:opcplc",
      "Timestamp": "2022-03-18T13:01:56.7551553Z",
      "Value": {
        "Value": 27259,
        "SourceTimestamp": "2022-03-18T13:01:56.7393301Z",
        "ServerTimestamp": "2022-03-18T13:01:56.7401032Z"
      },
      "SequenceNumber": 22,
      "ExtensionFields": {
        "PublisherId": "opc.tcp://opcplc:50000_9C43F84E",
        "DataSetWriterId": "1000"
      }
    },
    {
      "NodeId": "nsu=http://microsoft.com/Opc/OpcPlc/;s=RandomSignedInt32",
      "EndpointUrl": "opc.tcp://opcplc:50000/",
      "ApplicationUri": "urn:OpcPlc:opcplc",
      "Timestamp": "2022-03-18T13:01:56.7551553Z",
      "Value": {
        "Value": -2127202062,
        "SourceTimestamp": "2022-03-18T13:01:56.7393504Z",
        "ServerTimestamp": "2022-03-18T13:01:56.7398952Z"
      },
      "SequenceNumber": 22,
      "ExtensionFields": {
        "PublisherId": "opc.tcp://opcplc:50000_9C43F84E",
        "DataSetWriterId": "1000"
      }
    },
    {
      "NodeId": "nsu=http://microsoft.com/Opc/OpcPlc/;s=RandomUnsignedInt32",
      "EndpointUrl": "opc.tcp://opcplc:50000/",
      "ApplicationUri": "urn:OpcPlc:opcplc",
      "Timestamp": "2022-03-18T13:01:56.7551553Z",
      "Value": {
        "Value": 456421443,
        "SourceTimestamp": "2022-03-18T13:01:56.739439Z",
        "ServerTimestamp": "2022-03-18T13:01:56.7395003Z"
      },
      "SequenceNumber": 22,
      "ExtensionFields": {
        "PublisherId": "opc.tcp://opcplc:50000_9C43F84E",
        "DataSetWriterId": "1000"
      }
    },
    {
      "NodeId": "nsu=http://microsoft.com/Opc/OpcPlc/;s=BadFastUInt1",
      "EndpointUrl": "opc.tcp://opcplc:50000/",
      "ApplicationUri": "urn:OpcPlc:opcplc",
      "Timestamp": "2022-03-18T13:01:56.7551553Z",
      "Value": {
        "Value": 5,
        "SourceTimestamp": "2022-03-18T13:01:55.8426847Z",
        "ServerTimestamp": "2022-03-18T13:01:55.8427264Z"
      },
      "SequenceNumber": 22,
      "ExtensionFields": {
        "PublisherId": "opc.tcp://opcplc:50000_9C43F84E",
        "DataSetWriterId": "1000"
      }
    }
  ],
  "enqueuedTime": "Fri Mar 18 2022 14:01:56 GMT+0100 (Central European Standard Time)",
  "properties": {
    "$$ContentType": "application/x-monitored-item-json-v1",
    "iothub-message-schema": "application/json",
    "$$ContentEncoding": "utf-8"
  }
}
```

To provide compatible with the Connected Factory 1.0 and versions of OPC Publisher <= 2.5, OPC Publisher can be started in standalone mode with `--fm=false` argument:

```json
{
  "body": {
    "NodeId": "nsu=http://microsoft.com/Opc/OpcPlc/;s=StepUp",
    "EndpointUrl": "opc.tcp://opcplc:50000/",
    "Value": {
      "Value": 28679,
      "SourceTimestamp": "2022-03-18T13:04:18.7244388Z"
    }
  },
  "enqueuedTime": "Fri Mar 18 2022 14:04:18 GMT+0100 (Central European Standard Time)",
  "properties": {
    "$$ContentType": "application/x-monitored-item-json-v1",
    "iothub-message-schema": "application/json",
    "$$ContentEncoding": "utf-8"
  }
}
```

or

```json
{
  "body": {
    "NodeId": "nsu=http://microsoft.com/Opc/OpcPlc/;s=BadFastUInt1",
    "EndpointUrl": "opc.tcp://opcplc:50000/",
    "Value": {
      "Value": 4,
      "StatusCode": {
        "Symbol": "UncertainLastUsableValue",
        "Code": 1083179008
      },
      "SourceTimestamp": "2022-03-18T13:04:14.8405063Z"
    }
  },
  "enqueuedTime": "Fri Mar 18 2022 14:04:15 GMT+0100 (Central European Standard Time)",
  "properties": {
    "$$ContentType": "application/x-monitored-item-json-v1",
    "iothub-message-schema": "application/json",
    "$$ContentEncoding": "utf-8"
  }
}
```

In this case, if OPC Publisher is started in bulk mode with `--bs=5` argument a message would look like:

```json
{
  "body": [
    {
      "NodeId": "nsu=http://microsoft.com/Opc/OpcPlc/;s=AlternatingBoolean",
      "EndpointUrl": "opc.tcp://opcplc:50000/",
      "Value": {
        "Value": true,
        "SourceTimestamp": "2022-03-18T13:06:30.932775Z"
      }
    },
    {
      "NodeId": "nsu=http://microsoft.com/Opc/OpcPlc/;s=StepUp",
      "EndpointUrl": "opc.tcp://opcplc:50000/",
      "Value": {
        "Value": 30003,
        "SourceTimestamp": "2022-03-18T13:06:31.1337676Z"
      }
    },
    {
      "NodeId": "nsu=http://microsoft.com/Opc/OpcPlc/;s=RandomSignedInt32",
      "EndpointUrl": "opc.tcp://opcplc:50000/",
      "Value": {
        "Value": -2052144044,
        "SourceTimestamp": "2022-03-18T13:06:31.1338343Z"
      }
    },
    {
      "NodeId": "nsu=http://microsoft.com/Opc/OpcPlc/;s=RandomUnsignedInt32",
      "EndpointUrl": "opc.tcp://opcplc:50000/",
      "Value": {
        "Value": 186770890,
        "SourceTimestamp": "2022-03-18T13:06:31.1339985Z"
      }
    },
    {
      "NodeId": "nsu=http://microsoft.com/Opc/OpcPlc/;s=BadFastUInt1",
      "EndpointUrl": "opc.tcp://opcplc:50000/",
      "Value": {
        "StatusCode": {
          "Symbol": "BadNoCommunication",
          "Code": 2150694912
        },
        "SourceTimestamp": "2022-03-18T13:06:30.8423538Z"
      }
    }
  ],
  "enqueuedTime": "Fri Mar 18 2022 14:06:31 GMT+0100 (Central European Standard Time)",
  "properties": {
    "$$ContentType": "application/x-monitored-item-json-v1",
    "iothub-message-schema": "application/json",
    "$$ContentEncoding": "utf-8"
  }
}
```

## Telemetry messages generated by the Telemetry Processor in Orchestrated mode

These messages can be read from the Industrial IoT Platforms Event Hub when the entire platform is deployed. Event messages are not supported.

### Samples Mode

Message body is

```json
{
  "publisherId": "uat46f9f8f82fd5c1b42a7de31b5dc2c11ef418a62f",
  "dataSetClassId": "http://test.org/UA/Data/#i=10845",
  "dataSetWriterId": "uat46f9f8f82fd5c1b42a7de31b5dc2c11ef418a62f",
  "sequenceNumber": 0,
  "metaDataVersion": "1.0",
  "status": "Good",
  "timestamp": "2020-03-24T23:54:23.4955724Z",
  "payload": {
    "http://test.org/UA/Data/#i=10845": {
      "value": 27,
      "sourceTimestamp": "2020-03-24T23:54:23.1307846Z",
      "serverTimestamp": "2020-03-24T23:54:23.1307846Z"
    }
  }
}
```

### OPC UA PubSub Mode

Message body is

```json
{
  "messageId": "21",
  "publisherId": "uat46f9f8f82fd5c1b42a7de31b5dc2c11ef418a62f",
  "dataSetClassId": "78c4e91c-82cb-444e-a8e0-6bbacc9a946d",
  "dataSetWriterId": "uat46f9f8f82fd5c1b42a7de31b5dc2c11ef418a62f",
  "sequenceNumber": 21,
  "metaDataVersion": "1.1",
  "status": "Good",
  "timestamp": "2020-03-25T00:00:28.5713393Z",
  "payload": {
    "http://test.org/UA/Data/#i=10845": {
      "value": -91,
      "sourceTimestamp": "2020-03-25T00:00:28.0498921Z",
      "serverTimestamp": "2020-03-25T00:00:28.0498921Z"
    },
    "http://test.org/UA/Data/#i=10846": {
      "value": 89,
      "sourceTimestamp": "2020-03-25T00:00:28.0498921Z",
      "serverTimestamp": "2020-03-25T00:00:28.0498921Z"
    }
  }
}
```
