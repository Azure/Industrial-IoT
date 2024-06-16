# Telemetry Message Formats <!-- omit in toc -->

[Home](./readme.md)

> This documentation applies to version 2.9

## Table Of Contents <!-- omit in toc -->

- [Messaging Profiles supported by OPC Publisher](#messaging-profiles-supported-by-opc-publisher)
- [OPC UA Pub Sub Encoding](#opc-ua-pub-sub-encoding)
  - [Standards compliance](#standards-compliance)
  - [Delta and key frame messages](#delta-and-key-frame-messages)
  - [Event messages](#event-messages)
  - [Reversible encoding](#reversible-encoding)
  - [Pending Alarm snapshots](#pending-alarm-snapshots)
  - [Keep Alive messages](#keep-alive-messages)
- [Samples mode encoding (Legacy)](#samples-mode-encoding-legacy)
  - [Value change messages in Samples mode](#value-change-messages-in-samples-mode)
  - [Event messages in Samples mode](#event-messages-in-samples-mode)

OPC Publisher supports a rich set of message formats, including legacy formats supported.

## Messaging Profiles supported by OPC Publisher

| Messaging Mode<br>(--mm) | Message Encoding<br>(--me) | NetworkMessageContentMask | DataSetMessageContentMask | DataSetFieldContentMask | Metadata supported | KeyFrames supported | KeepAlive supported | Schema publishing |
   |--------------------------|----------------------------|---------------------------|---------------------------|-------------------------|--------------------|---------------------|---------------------|-------------------|
| Samples | Json | DataSetMessageHeader, MonitoredItemMessage<br>(0x2) | MetaDataVersion, MajorVersion, MinorVersion, MessageType, DataSetWriterName<br>(0xB0000062) | StatusCode, SourceTimestamp, NodeId, DisplayName, EndpointUrl<br>(0x3) |   |   |   |
| FullSamples | Json | DataSetMessageHeader, MonitoredItemMessage<br>(0x2) | Timestamp, MetaDataVersion, DataSetWriterId, MajorVersion, MinorVersion, SequenceNumber, MessageType, DataSetWriterName<br>(0xF200006F) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) |   |   |   |
| PubSub | Json | PublisherId, WriterGroupId, NetworkMessageNumber, SequenceNumber, PayloadHeader, Timestamp, DataSetClassId, NetworkMessageHeader, DataSetMessageHeader<br>(0x1B) | MetaDataVersion, MajorVersion, MinorVersion, MessageType, DataSetWriterName<br>(0xB0000062) | StatusCode, SourceTimestamp, NodeId, DisplayName, EndpointUrl<br>(0x3) | X | X | X |
| FullNetworkMessages | Json | PublisherId, WriterGroupId, NetworkMessageNumber, SequenceNumber, PayloadHeader, Timestamp, DataSetClassId, NetworkMessageHeader, DataSetMessageHeader<br>(0x1B) | Timestamp, MetaDataVersion, DataSetWriterId, MajorVersion, MinorVersion, SequenceNumber, MessageType, DataSetWriterName<br>(0xF200006F) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) | X | X | X |
| PubSub | JsonGzip | PublisherId, WriterGroupId, NetworkMessageNumber, SequenceNumber, PayloadHeader, Timestamp, DataSetClassId, NetworkMessageHeader, DataSetMessageHeader<br>(0x1B) | MetaDataVersion, MajorVersion, MinorVersion, MessageType, DataSetWriterName<br>(0xB0000062) | StatusCode, SourceTimestamp, NodeId, DisplayName, EndpointUrl<br>(0x3) | X | X | X |
| FullNetworkMessages | JsonGzip | PublisherId, WriterGroupId, NetworkMessageNumber, SequenceNumber, PayloadHeader, Timestamp, DataSetClassId, NetworkMessageHeader, DataSetMessageHeader<br>(0x1B) | Timestamp, MetaDataVersion, DataSetWriterId, MajorVersion, MinorVersion, SequenceNumber, MessageType, DataSetWriterName<br>(0xF200006F) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) | X | X | X |
| PubSub | JsonReversible | PublisherId, WriterGroupId, NetworkMessageNumber, SequenceNumber, PayloadHeader, Timestamp, DataSetClassId, NetworkMessageHeader, DataSetMessageHeader<br>(0x1B) | MetaDataVersion, MajorVersion, MinorVersion, MessageType, DataSetWriterName, ReversibleFieldEncoding<br>(0xB00000E2) | StatusCode, SourceTimestamp, NodeId, DisplayName, EndpointUrl<br>(0x3) | X | X | X |
| PubSub | JsonReversibleGzip | PublisherId, WriterGroupId, NetworkMessageNumber, SequenceNumber, PayloadHeader, Timestamp, DataSetClassId, NetworkMessageHeader, DataSetMessageHeader<br>(0x1B) | MetaDataVersion, MajorVersion, MinorVersion, MessageType, DataSetWriterName, ReversibleFieldEncoding<br>(0xB00000E2) | StatusCode, SourceTimestamp, NodeId, DisplayName, EndpointUrl<br>(0x3) | X | X | X |
| FullNetworkMessages | JsonReversible | PublisherId, WriterGroupId, NetworkMessageNumber, SequenceNumber, PayloadHeader, Timestamp, DataSetClassId, NetworkMessageHeader, DataSetMessageHeader<br>(0x1B) | Timestamp, MetaDataVersion, DataSetWriterId, MajorVersion, MinorVersion, SequenceNumber, MessageType, DataSetWriterName, ReversibleFieldEncoding<br>(0xF20000EF) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) | X | X | X |
| FullNetworkMessages | JsonReversibleGzip | PublisherId, WriterGroupId, NetworkMessageNumber, SequenceNumber, PayloadHeader, Timestamp, DataSetClassId, NetworkMessageHeader, DataSetMessageHeader<br>(0x1B) | Timestamp, MetaDataVersion, DataSetWriterId, MajorVersion, MinorVersion, SequenceNumber, MessageType, DataSetWriterName, ReversibleFieldEncoding<br>(0xF20000EF) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) | X | X | X |
| Samples | JsonReversible | DataSetMessageHeader, MonitoredItemMessage<br>(0x2) | MetaDataVersion, MajorVersion, MinorVersion, MessageType, DataSetWriterName, ReversibleFieldEncoding<br>(0xB00000E2) | StatusCode, SourceTimestamp, NodeId, DisplayName, EndpointUrl<br>(0x3) |   |   |   |
| Samples | JsonReversibleGzip | DataSetMessageHeader, MonitoredItemMessage<br>(0x2) | MetaDataVersion, MajorVersion, MinorVersion, MessageType, DataSetWriterName, ReversibleFieldEncoding<br>(0xB00000E2) | StatusCode, SourceTimestamp, NodeId, DisplayName, EndpointUrl<br>(0x3) |   |   |   |
| FullSamples | JsonReversible | DataSetMessageHeader, MonitoredItemMessage<br>(0x2) | Timestamp, MetaDataVersion, DataSetWriterId, MajorVersion, MinorVersion, SequenceNumber, MessageType, DataSetWriterName, ReversibleFieldEncoding<br>(0xF20000EF) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) |   |   |   |
| FullSamples | JsonReversibleGzip | DataSetMessageHeader, MonitoredItemMessage<br>(0x2) | Timestamp, MetaDataVersion, DataSetWriterId, MajorVersion, MinorVersion, SequenceNumber, MessageType, DataSetWriterName, ReversibleFieldEncoding<br>(0xF20000EF) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) |   |   |   |
| DataSetMessages | Json | DataSetMessageHeader<br>(0x2) | Timestamp, MetaDataVersion, DataSetWriterId, MajorVersion, MinorVersion, SequenceNumber, MessageType, DataSetWriterName<br>(0xF200006F) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) | X | X | X |
| DataSetMessages | JsonGzip | DataSetMessageHeader<br>(0x2) | Timestamp, MetaDataVersion, DataSetWriterId, MajorVersion, MinorVersion, SequenceNumber, MessageType, DataSetWriterName<br>(0xF200006F) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) | X | X | X |
| DataSetMessages | JsonReversible | DataSetMessageHeader<br>(0x2) | Timestamp, MetaDataVersion, DataSetWriterId, MajorVersion, MinorVersion, SequenceNumber, MessageType, DataSetWriterName, ReversibleFieldEncoding<br>(0xF20000EF) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) | X | X | X |
| DataSetMessages | JsonReversibleGzip | DataSetMessageHeader<br>(0x2) | Timestamp, MetaDataVersion, DataSetWriterId, MajorVersion, MinorVersion, SequenceNumber, MessageType, DataSetWriterName, ReversibleFieldEncoding<br>(0xF20000EF) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) | X | X | X |
| SingleDataSetMessage | Json | DataSetMessageHeader, SingleDataSetMessage<br>(0x6) | Timestamp, MetaDataVersion, DataSetWriterId, MajorVersion, MinorVersion, SequenceNumber, MessageType, DataSetWriterName<br>(0xF200006F) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) | X | X | X |
| SingleDataSetMessage | JsonGzip | DataSetMessageHeader, SingleDataSetMessage<br>(0x6) | Timestamp, MetaDataVersion, DataSetWriterId, MajorVersion, MinorVersion, SequenceNumber, MessageType, DataSetWriterName<br>(0xF200006F) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) | X | X | X |
| SingleDataSetMessage | JsonReversible | DataSetMessageHeader, SingleDataSetMessage<br>(0x6) | Timestamp, MetaDataVersion, DataSetWriterId, MajorVersion, MinorVersion, SequenceNumber, MessageType, DataSetWriterName, ReversibleFieldEncoding<br>(0xF20000EF) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) | X | X | X |
| SingleDataSetMessage | JsonReversibleGzip | DataSetMessageHeader, SingleDataSetMessage<br>(0x6) | Timestamp, MetaDataVersion, DataSetWriterId, MajorVersion, MinorVersion, SequenceNumber, MessageType, DataSetWriterName, ReversibleFieldEncoding<br>(0xF20000EF) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) | X | X | X |
| DataSets | Json | 0<br>(0x0) | 0<br>(0xF2000000) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) | X | X | X |
| DataSets | JsonGzip | 0<br>(0x0) | 0<br>(0xF2000000) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) | X | X | X |
| SingleDataSet | Json | SingleDataSetMessage<br>(0x4) | 0<br>(0xF2000000) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) | X | X | X |
| SingleDataSet | JsonGzip | SingleDataSetMessage<br>(0x4) | 0<br>(0xF2000000) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) | X | X | X |
| DataSets | JsonReversible | 0<br>(0x0) | 0<br>(0xF2000000) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) | X | X | X |
| DataSets | JsonReversibleGzip | 0<br>(0x0) | 0<br>(0xF2000000) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) | X | X | X |
| SingleDataSet | JsonReversible | SingleDataSetMessage<br>(0x4) | 0<br>(0xF2000000) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) | X | X | X |
| SingleDataSet | JsonReversibleGzip | SingleDataSetMessage<br>(0x4) | 0<br>(0xF2000000) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) | X | X | X |
| RawDataSets | Json | 0<br>(0x0) | 0<br>(0x0) | RawData<br>(0x20) |   | X | X |
| RawDataSets | JsonGzip | 0<br>(0x0) | 0<br>(0x0) | RawData<br>(0x20) |   | X | X |
| SingleRawDataSet | Json | SingleDataSetMessage<br>(0x4) | 0<br>(0x0) | RawData<br>(0x20) | X | X | X |
| SingleRawDataSet | JsonGzip | SingleDataSetMessage<br>(0x4) | 0<br>(0x0) | RawData<br>(0x20) | X | X | X |
| RawDataSets | JsonReversible | 0<br>(0x0) | 0<br>(0x0) | RawData<br>(0x20) |   | X | X |
| RawDataSets | JsonReversibleGzip | 0<br>(0x0) | 0<br>(0x0) | RawData<br>(0x20) |   | X | X |
| SingleRawDataSet | JsonReversible | SingleDataSetMessage<br>(0x4) | 0<br>(0x0) | RawData<br>(0x20) | X | X | X |
| SingleRawDataSet | JsonReversibleGzip | SingleDataSetMessage<br>(0x4) | 0<br>(0x0) | RawData<br>(0x20) | X | X | X |
| PubSub | Uadp | PublisherId, WriterGroupId, NetworkMessageNumber, SequenceNumber, PayloadHeader, Timestamp, DataSetClassId, NetworkMessageHeader, DataSetMessageHeader<br>(0x2F5) | MetaDataVersion, MajorVersion, MinorVersion, MessageType, DataSetWriterName<br>(0x18) | StatusCode, SourceTimestamp, NodeId, DisplayName, EndpointUrl<br>(0x3) | X | X | X |
| FullNetworkMessages | Uadp | PublisherId, WriterGroupId, NetworkMessageNumber, SequenceNumber, PayloadHeader, Timestamp, DataSetClassId, NetworkMessageHeader, DataSetMessageHeader<br>(0x2F5) | Timestamp, MetaDataVersion, DataSetWriterId, MajorVersion, MinorVersion, SequenceNumber, MessageType, DataSetWriterName<br>(0x39) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) | X | X | X |
| DataSetMessages | Uadp | DataSetMessageHeader<br>(0x0) | Timestamp, MetaDataVersion, DataSetWriterId, MajorVersion, MinorVersion, SequenceNumber, MessageType, DataSetWriterName<br>(0x39) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) | X | X | X |
| SingleDataSetMessage | Uadp | DataSetMessageHeader, SingleDataSetMessage<br>(0x0) | Timestamp, MetaDataVersion, DataSetWriterId, MajorVersion, MinorVersion, SequenceNumber, MessageType, DataSetWriterName<br>(0x39) | StatusCode, SourceTimestamp, ServerTimestamp, NodeId, DisplayName, EndpointUrl, ApplicationUri, ExtensionFields<br>(0x7) | X | X | X |
| RawDataSets | Uadp | 0<br>(0x0) | 0<br>(0x0) | RawData<br>(0x20) |   | X | X |
| SingleRawDataSet | Uadp | SingleDataSetMessage<br>(0x0) | 0<br>(0x0) | RawData<br>(0x20) | X | X | X |

## OPC UA Pub Sub Encoding

To use the OPC UA PubSub format specify a value for `--mm` on the command line. This needs to be done because the OPC publisher defaults to `--mm=Samples` mode which existed before the introduction of OPC UA standards compliant PubSub format. You should always use PubSub format specified in the OPC UA Standard. We will not support the non standards compliant Samples mode in versions greater than 2.*.

### Standards compliance

OPC Publisher 2.9 and above supports strict adherence to Part 6 and Part 14 of the OPC UA specification when it comes to network message encoding. To enable strict mode use the `-c` or `--strict` [command line](./commandline.md) option. For backwards compatibility this option is off by default.

> It is highly recommended to always run OPC Publisher with strict adherence turned on. Strict mode is continuously adjusted as we do interoperability testing and parts of the standard are clarified.  

The following are the key differences between strict compliance and the compatibility mode with previous versions of OPC Publisher:

| Strict mode | Compatibility mode |
|-------------|--------------------|
| `PubSub` is the default encoding mode if nothing else is specified | `Samples` is the default encoding mode and `--mm=PubSub` must be explicitly specified |
| `DataSetWriterId` is a unique integer in the `DataSetWriterGroup` | `DataSetWriterId` is the writer name string |
| `DataSetWriterName` is the writer name string | `DataSetWriterName` is not used |
| Network messages contain array of data set messages when batching | Array of network messages is sent when batching |
| `Status` field is status code integer only (as per Part 14) | `Status` is fully JSON encoded Status code (per Part 6) |
| JSON encoding compliant with Part 6 | Microsoft [JSON extensions](../json.md) |

### Delta and key frame messages

This section covers data value change messages (Message type `ua-deltaframe` and `ua-keyframe`). Details for OPC UA Alarms and Events messages (`ua-event`) can be found [further on](#event-messages).

The following messages are emitted for data value changes in a subscription if `--mm=PubSub` message mode is used with `--me=Json`:

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

The data set messages in the `ua-data` network message can be delta frames (`ua-deltaframe`, containing only changed values in the dataset), key frames (`ua-keyframe`, containing all values of the dataset), keep alive messages (`ua-keepalive`, containing no payload), or [events and conditions](#event-messages).

IMPORTANT: Depending on the number of nodes in a subscription and the data type of properties inside a single dataset, data set messages contained in a network message can be very large.  Indeed, it can potentially be too large and not fit into IoT Hub Messages which are limited to 256 kB.  In this case messages might not be sent. You can try to use `--me=JsonGzip` to compress messages using Gzip compression, or use `--me=Uadp` which supports network message chuncing (and overcomes any transport limitation). If neither help or are an option it is recommended to create smaller subscriptions (by adding less nodes to an endpoint) or disable dataset metadata message sending using `--dm=False`.

The data set is described by a corresponding metadata message (message type `ua-metdata`), which is emitted prior to the first message and whenever the configuration is updated requiring an update of the metadata. Metadata can also be sent periodically, which can be configured using the control plane of OPC Publisher.

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

IMPORTANT: Depending on the number of nodes in a subscription, a Metadata messages can be very large.  Indeed, it can potentially be too large and not fit into IoT Hub Messages which are limited to 256 kB.  In this case they are created but never sent. You can choose to use `--me=JsonGzip` to compress messages using Gzip compression, or use `--me=Uadp` which supports network message chunking. If neither help or are an option it is recommended to create smaller subscriptions (by adding less nodes to an endpoint) or disable dataset metadata message sending using `--dm=False`.

### Event messages

This section describes what the output looks like when listening for events in the OPC Publisher.

To use the OPC UA PubSub format specify the `--mm=PubSub` command line. This needs to be done because the OPC publisher defaults to `--mm=Samples` [mode](#samples-mode-encoding-legacy) which existed before the introduction of OPC UA standards compliant PubSub format.

Events should be produced in the PubSub format specified in the OPC UA Standard. The payload is an event which consists of fields selected in the select clause and its values.

The following is an example of the output you will se when listening to events from the Simple Events sample:

```json
{
  "body": [
    {
      "MessageId": "43",
      "MessageType": "ua-data",
      "PublisherId": "SIMPLE-EVENTS",
      "DataSetWriterGroup": "SIMPLE-EVENTS",
      "Messages": [
        {
          "DataSetWriterId": "SIMPLE-EVENTS",
          "MetaDataVersion": {
            "MajorVersion": 1222304427,
            "MinorVersion": 801860751
          },
          "MessageType": "ua-event",
          "Payload": {
            "EventId": "+6CQjN1eqkO6+yHJnxMz5w==",
            "EventType": "http://microsoft.com/Opc/OpcPlc/SimpleEvents#i=14",
            "Message": "The system cycle '59' has started.",
            "ReceiveTime": "2021-06-21T12:38:55.5814091Z",
            "Severity": 1,
            "SourceName": "System",
            "SourceNode": "i=2253",
            "http://opcfoundation.org/SimpleEvents#CurrentStep": {
              "Name": "Step 1",
              "Duration": 1000.0
            },
            "Time": "2021-06-21T12:38:55.5814078Z"
          }
        }
      ]
    }
  ],
  "enqueuedTime": "Mon Jan 21 2023 14:39:02 GMT+0200 (Central European Summer Time)",
  "properties": {
    "$$ContentType": "application/x-network-message-json-v1",
    "iothub-message-schema": "application/ua+json",
    "$$ContentEncoding": "utf-8"
  }
}
```

The event is described by the corresponding metadata message, which is emitted prior to the first message and whenever the configuration is updated requiring an update of the metadata. Metadata can also be sent periodically, which can be configured using the control plane of OPC Publisher. The following metadata is provided in `--strict` mode:

```json
{
  "body": [
    {
      "MessageId": "edecf7ec-5ae8-4957-82ef-7f915dddb5be",
      "MessageType": "ua-metadata",
      "PublisherId": "opc.tcp://localhost:55924/UA/SampleServer_E8BAB2AD",
      "DataSetWriterId": 1,
      "MetaData": {
        "Namespaces": [
          "http://opcfoundation.org/UA/",
          "http://test.org/UA/Data/",
          "http://test.org/UA/Data//Instance",
          "http://opcfoundation.org/UA/Boiler//Instance",
          "urn:localhost:somecompany.com:VehiclesServer",
          "http://opcfoundation.org/UA/Vehicles/Types",
          "http://opcfoundation.org/UA/Vehicles/Instances",
          "http://opcfoundation.org/ReferenceApplications",
          "http://opcfoundation.org/UA/Diagnostics",
          "http://opcfoundation.org/UA/Boiler/"
        ],
        "StructureDataTypes": [
          {
            "DataTypeId": {
              "Id": 183,
              "Namespace": "http://opcfoundation.org/SimpleEvents"
            },
            "Name": {
              "Name": "CycleStepDataType",
              "Uri": "http://opcfoundation.org/SimpleEvents"
            },
            "StructureDefinition": {
              "BaseDataType": {
                "Id": 22
              },
              "StructureType": "Structure_0",
              "Fields": [
                {
                  "Name": "Name",
                  "DataType": {
                    "Id": 12
                  },
                  "ValueRank": -1,
                  "ArrayDimensions": [],
                  "MaxStringLength": 0,
                  "IsOptional": false
                },
                {
                  "Name": "Duration",
                  "DataType": {
                    "Id": 11
                  },
                  "ValueRank": -1,
                  "ArrayDimensions": [],
                  "MaxStringLength": 0,
                  "IsOptional": false
                }
              ]
            }
          }
        ],
        "EnumDataTypes": [],
        "SimpleDataTypes": [],
        "Fields": [
          {
            "Name": "EventId",
            "FieldFlags": 0,
            "BuiltInType": 15,
            "DataType": {
              "Id": 15
            },
            "ValueRank": -1,
            "ArrayDimensions": [],
            "MaxStringLength": 0,
            "DataSetFieldId": "487f710c-9f43-4425-9a77-03f3396362f7",
            "Properties": []
          },
          {
            "Name": "Message",
            "FieldFlags": 0,
            "BuiltInType": 21,
            "DataType": {
              "Id": 21
            },
            "ValueRank": -1,
            "ArrayDimensions": [],
            "MaxStringLength": 0,
            "DataSetFieldId": "15c7bc3a-4714-4f5e-9874-d4671288f5a0",
            "Properties": []
          },
          {
            "Name": "http://opcfoundation.org/SimpleEvents#CycleId",
            "FieldFlags": 0,
            "BuiltInType": 12,
            "DataType": {
              "Id": 12
            },
            "ValueRank": -1,
            "ArrayDimensions": [],
            "MaxStringLength": 0,
            "DataSetFieldId": "03378140-f21b-4ee1-9bbe-01325e847128",
            "Properties": []
          },
          {
            "Name": "http://opcfoundation.org/SimpleEvents#CurrentStep",
            "FieldFlags": 0,
            "BuiltInType": 22,
            "DataType": {
              "Id": 183,
              "Namespace": "http://opcfoundation.org/SimpleEvents"
            },
            "ValueRank": -1,
            "ArrayDimensions": [],
            "MaxStringLength": 0,
            "DataSetFieldId": "a9cd0d57-ae64-4e20-b113-b3df52cb6a59",
            "Properties": []
          }
        ],
        "ConfigurationVersion": {
          "MajorVersion": 1222308210,
          "MinorVersion": 2861644214
        }
      },
      "DataSetWriterName": "1000"
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

IMPORTANT: Depending on the number of members in an event type and their data types, data set messages contained in a network message can be large.  In some cases a JSON metadata message can potentially be too large and not fit into IoT Hub Messages which are limited to 256 kB. In this case messages might not be sent. You can try to use `--me=JsonGzip` to compress event data set messages using Gzip compression, or use `--me=Uadp` which supports network message chunking (and overcomes any transport limitation). If neither help or are an option it is recommended to use an event filter and select the properties needed or disable dataset metadata message sending altogether using `--dm=False`.

### Reversible encoding

The format produced here does not contain enough information to decode the message using the OPC UA type system. If you need to decode messages using a OPC UA JSON decoder the command-line option called `UseReversibleEncoding` can be set to `true`. If you enable this setting the output will look like as follows:

```json
{
  "body": [
    {
      "MessageId": "5",
      "MessageType": "ua-data",
      "PublisherId": "opc.tcp://localhost:54340/UA/SampleServer_5CB8F1A5",
      "Messages": [
        {
          "DataSetWriterId": "SIMPLE-EVENTS",
          "MetaDataVersion": {
            "MajorVersion": 1222304426,
            "MinorVersion": 3462403799
          },
          "MessageType": "ua-event",
          "Payload": {
            "EventId": {
              "Type": "ByteString",
              "Body": "88C2T817uUWMVNDclyOFnA=="
            },
            "Message": {
              "Type": "LocalizedText",
              "Body": {
                "Text": "The system cycle \u00275\u0027 has started.",
                "Locale": "en-US"
              }
            },
            "http://opcfoundation.org/SimpleEvents#CycleId": {
              "Type": "String",
              "Body": "5"
            },
            "http://opcfoundation.org/SimpleEvents#CurrentStep": {
              "Type": "ExtensionObject",
              "Body": {
                "TypeId": "http://opcfoundation.org/SimpleEvents#i=183",
                "Encoding": "Json",
                "Body": {
                  "Name": "Step 1",
                  "Duration": 1000.0
                }
              }
            }
          }
        }
      ]
    }
  ],
  "enqueuedTime": "Mon Jun 21 2021 14:45:22 GMT+0200 (Central European Summer Time)",
  "properties": {
    "$$ContentType": "application/x-network-message-json-v1",
    "iothub-message-schema": "application/ua+json",
    "$$ContentEncoding": "utf-8"
  }
}
```

This JSON contains the metadata information to decode each variant value.

### Pending Alarm snapshots

The OPC Publisher also supports sending Pending Alarms (or conditions) which are events that are associated with a condition, as described in the user guide for [configuration of events](./readme.md#configuring-event-subscriptions). When this feature is enabled, it will listen to all ConditionType derived events and cache all that have has the `Retain` property set to true. It will then periodically generate output to broadcast the condition case still being in effect.

When running against the OPC Foundation's Alarms & Conditions reference server sample the output will look like this:

```json
{
  "body": [
    {
      "MessageId": "34",
      "MessageType": "ua-data",
      "PublisherId": "PENDING-ALARMS",
      "DataSetWriterGroup": "PENDING-ALARMS",
      "Messages": [
        {
          "DataSetWriterId": "PENDING-ALARMS",
          "MetaDataVersion": {
            "MajorVersion": 1,
            "MinorVersion": 0
          },
          "MessageType": "ua-condition",
          "Payload": {
            "EventId": "PQpa0fNNwUym272/HW40ww==",
            "EventType": "i=2830",
            "LocalTime": {
              "Offset": 60,
              "DaylightSavingInOffset": true
            },
            "Message": "The dialog was activated",
            "ReceiveTime": "2022-12-20T17:03:02.1338153Z",
            "Severity": 100,
            "SourceName": "EastTank",
            "SourceNode": "http://opcfoundation.org/AlarmCondition#s=1%3aColours%2fEastTank",
            "Time": "2022-12-20T17:03:02.1338153Z"
          }
        }
      ]
    }
  ],
  "enqueuedTime": "Mon Jun 21 2021 14:56:53 GMT+0200 (Central European Summer Time)",
  "properties": {
    "$$ContentType": "application/x-network-message-json-v1",
    "iothub-message-schema": "application/ua+json",
    "$$ContentEncoding": "utf-8"
  }
}
```

The important part to highlight here is that the payload is an array of events which have the Retain property set to true. Otherwise it's very similar to value change messages earlier.

### Keep Alive messages

Keep alive messages must be explicitly enabled (since they potentially consume bandwidth and cost). To Enable them for the OPC Publisher use the `--ka` [command line](./commandline.md) argument or enable it for a specific `DataSetWriter` in the [configuration](./readme.md#configuration-schema). [Samples](#samples-mode-encoding-legacy) mode does not support keep alive messages even when enabled.

Keep alive messages are part of the network message. A network message can contain more data sets from other writers that are also keep alive messages or of other message types.  A simple keep alive message is shown here:

```json
{
  "body": {
    "MessageId": "64",
    "MessageType": "ua-data",
    "PublisherId": "MyPublisher",
    "Messages": [
      {
        "DataSetWriterId": 1,
        "DataSetWriterName": "DataSet33",
        "SequenceNumber": 66,
        "MetaDataVersion": {
          "MajorVersion": 1,
          "MinorVersion": 0
        },
        "MessageType": "ua-keepalive",
        "Timestamp": "2023-03-18T12:55:21.3423234Z"
      }
    ]
  }
}
```

## Samples mode encoding (Legacy)

> IMPORTANT: Legacy `Samples` encoding mode is a message format that predates OPC UA PubSub message encoding and is thus considered legacy and not standards conform. We might decide to not support the non standards compliant Samples mode in future versions of OPC Publisher.

### Value change messages in Samples mode

In samples mode value change messages look like this:

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

The following message is an example with batching/bulk mode enabled. Here OPC Publisher was started with the `--bs=5` argument, where 5 is the number of messages to be batched.

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

### Event messages in Samples mode

The following sample messages show how events look like in legacy samples mode:

```json
{
  "body": {
    "NodeId": "i=2253",
    "EndpointUrl": "opc.tcp://localhost:57965/UA/SampleServer",
    "DisplayName": "SimpleEvents",
    "Value": {
      "EventId": "JdRhF43ktkKvJBrk\u002BsePkg==",
      "Message": "The system cycle \u00271\u0027 has started.",
      "http://opcfoundation.org/SimpleEvents#CycleId": "1",
      "http://opcfoundation.org/SimpleEvents#CurrentStep": {
        "Name": "Step 1",
        "Duration": 1000.0
      }
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

With `--fm=True` enabling full featured messages, these would then look like:

```json
{
  "body": {
    "NodeId": "i=2253",
    "EndpointUrl": "opc.tcp://localhost:56769/UA/SampleServer",
    "ApplicationUri": "urn:SampleServer",
    "DisplayName": "SimpleEvents",
    "Timestamp": "2022-12-05T11:00:18.1907826Z",
    "Value": {
      "EventId": "MB0Xs/BZ5US/BeKOUtsL8A==",
      "Message": "The system cycle \u00273\u0027 has started.",
      "http://opcfoundation.org/SimpleEvents#CycleId": "3",
      "http://opcfoundation.org/SimpleEvents#CurrentStep": {
        "Name": "Step 1",
        "Duration": 1000.0
      }
    },
    "SequenceNumber": 2,
    "ExtensionFields": {
      "PublisherId": "opc.tcp://localhost:56769/UA/SampleServer_59F0BDE1",
      "DataSetWriterId": "1000"
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

Pending Alarms (or conditions) sent in Samples mode look as follows:

```json
{
  "body": {
    "NodeId": "i=2253",
    "EndpointUrl": "opc.tcp://localhost:56692/UA/SampleServer",
    "DisplayName": "PendingAlarms",
    "Value": {
      "EventId": "xW5uvGPSuUWBdvp8IfSueQ==",
      "EventType": "i=2830",
      "LocalTime": {
        "Offset": 60,
        "DaylightSavingInOffset": true
      },
      "Message": "The dialog was activated",
      "ReceiveTime": "2022-12-20T15:53:10.3815705Z",
      "Severity": 100,
      "SourceName": "EastTank",
      "SourceNode": "http://opcfoundation.org/AlarmCondition#s=1%3aColours%2fEastTank",
      "Time": "2022-12-20T15:53:10.3815705Z"
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

Finally, using reversable mode, legacy samples messages will look as follows:

```json
{
  "body":   {
    "NodeId": "i=2253",
    "EndpointUrl": "opc.tcp://localhost:54040/UA/SampleServer",
    "DisplayName": "SimpleEvents",
    "Value": {
      "Type": "ExtensionObject",
      "Body": {
        "TypeId": "http://microsoft.com/Industrial-IoT/OpcPublisher#i=1",
        "Encoding": "Json",
        "Body": {
          "EventId": {
            "Type": "ByteString",
            "Body": "xbAm3QTXwEKsVZFcsHSdzA=="
          },
          "Message": {
            "Type": "LocalizedText",
            "Body": {
              "Text": "The system cycle \u00271\u0027 has started.",
              "Locale": "en-US"
            }
          },
          "http://opcfoundation.org/SimpleEvents#CycleId": {
            "Type": "String",
            "Body": "1"
          },
          "http://opcfoundation.org/SimpleEvents#CurrentStep": {
            "Type": "ExtensionObject",
            "Body": {
              "TypeId": "http://opcfoundation.org/SimpleEvents#i=183",
              "Encoding": "Json",
              "Body": {
                "Name": "Step 1",
                "Duration": 1000.0
              }
            }
          }
        }
      }
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
