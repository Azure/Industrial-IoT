# Detailing the output for OPC UA Events in OPC Publisher

[Home](../../readme.md)

This section describes what the output looks like when listening for events in the OPC Publisher.

To use the OPC UA PubSub format specify the `--mm=PubSub` command line. This needs to be done because the OPC publisher defaults to `--mm=Samples` [mode](#event-messages-in-samples-mode) which existed before the introduction of OPC UA standards compliant PubSub format.

Events should be produced in the PubSub format specified in the OPC UA Standard. The payload is an event which consists of fields selected in the select clause and its values. Details for data messages can be found [in this seperate document](./telemetry-messages-format.md).

## JSON encoded events in OPC UA PubSub Mode

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

The OPC Publisher also supports sending Pending Alarms (or conditions) which are events that are associated with a condition, as described in the user guide for [configuration of events](./publisher-event-configuration.md). When this feature is enabled, it will listen to all ConditionType derived events and cache all that have has the `Retain` property set to true. It will then periodically generate output to broadcast the condition case still being in effect. 

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

The important part to highlight here is that the payload is an array of events which have the Retain property set to true. Otherwise it's very similar to the regular [telematry messages](./telemetry-messages-format.md).

## Event messages in Samples Mode

> IMPORTANT: Legacy `Samples` encoding mode is a message format that predates OPC UA Pub Sub message encoding and is thus considered legacy and not standards conform. We might decide to not support the non standards compliant Samples mode in future versions of OPC Publisher.

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

The value is an *EncodeableDictionary* object, as indicated by the the type identifier: *http://microsoft.com/Industrial-IoT/OpcPublisher#i=1*. You can consume the type by getting a copy of the [*EncodeableDictionary*](../components/opc-ua/src/Microsoft.Azure.IIoT.OpcUa.Protocol/src/Stack/Encoders/Models/EncodeableDictionary.cs) class and registering it in the *ServiceMessageContext*. Then, the *JsonDecoderEx* can properly decode the EncodeableDictionary:

```cs
var serviceMessageContext = new ServiceMessageContext();
serviceMessageContext.Factory.AddEncodeableType(typeof(EncodeableDictionary));

using (var stream = new MemoryStream(buffer)) {
    using var decoder = new JsonDecoderEx(stream, serviceMessageContext);
    var data = new EncodeableDictionary();
    data.Decode(decoder);

    ...
```

