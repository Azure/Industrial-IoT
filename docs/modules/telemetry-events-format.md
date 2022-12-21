# Detailing the output for OPC UA Events in OPC Publisher

[Home](../../readme.md)

This section describes what the output looks like when listening for events in the OPC Publisher.

To use the OPC UA PubSub format specify the `--mm=PubSub` command line. This needs to be done because the OPC publisher defaults to `--mm=Samples` mode which existed before the introduction of OPC UA standards compliant PubSub format.

Events should be produced in the PubSub format specified in the OPC UA Standard. We might decide to not support the non standards compliant Samples mode in future versions of OPC Publisher. The payload is an event which consists of fields selected in the select clause and its values. Details for data messages can be found [in this seperate document](./telemetry-messages-format.md).

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
            "MajorVersion": 1,
            "MinorVersion": 0
          },
          "Payload": {
            "SimpleEvents": {
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
        }
      ]
    }
  ],
  "enqueuedTime": "Mon Jun 21 2021 14:39:02 GMT+0200 (Central European Summer Time)",
  "properties": {
    "$$ContentType": "application/x-network-message-json-v1",
    "iothub-message-schema": "application/ua+json",
    "$$ContentEncoding": "utf-8"
  }
}
```

The event in the payload is named "SimpleEvents" which was configured by setting the DisplayName property of the `OPCNodes` configuration. Also note that all fields and values reside under the Value key.

### Reversible encoding

The format produced here does not contain enough information to decode the message using the OPC UA type system. If you need to decode messages using a OPC UA JSON decoder the command-line option called `UseReversibleEncoding` can be set to `true`. If you enable this setting the output will look like as follows:

```json
{
  "body": [
    {
      "MessageId": "15",
      "MessageType": "ua-data",
      "PublisherId": "SIMPLE-EVENTS",
      "DataSetWriterGroup": "SIMPLE-EVENTS",
      "Messages": [
        {
          "DataSetWriterId": "SIMPLE-EVENTS",
          "MetaDataVersion": {
            "MajorVersion": 1,
            "MinorVersion": 0
          },
          "Payload": {
            "SimpleEvents": {
              "Value": {
                "Type": "ExtensionObject",
                "Body": {
                  "TypeId": "http://microsoft.com/Industrial-IoT/OpcPublisher#i=1",
                  "Encoding": "Json",
                  "Body": {
                    "EventId": {
                      "Type": "ByteString",
                      "Body": "DYg+y18fTE6jpQNTu9KB7A=="
                    },
                    "EventType": {
                      "Type": "NodeId",
                      "Body": "http://microsoft.com/Opc/OpcPlc/SimpleEvents#i=14"
                    },
                    "Message": {
                      "Type": "LocalizedText",
                      "Body": {
                        "Text": "The system cycle '318' has started.",
                        "Locale": "en-US"
                      }
                    },
                    "ReceiveTime": {
                      "Type": "DateTime",
                      "Body": "2021-06-21T12:45:22.5819817Z"
                    },
                    "Severity": {
                      "Type": "UInt16",
                      "Body": 2
                    },
                    "SourceName": {
                      "Type": "String",
                      "Body": "System"
                    },
                    "SourceNode": {
                      "Type": "NodeId",
                      "Body": "i=2253"
                    },
                    "Time": {
                      "Type": "DateTime",
                      "Body": "2021-06-21T12:45:22.5819815Z"
                    }
                  }
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

This JSON contains enough metadata information to decode it properly.

The output is contained within an *EncodeableDictionary* object, as indicated by the the type identifier: *http://microsoft.com/Industrial-IoT/OpcPublisher#i=1*. At this point, the best way to consume the type is by getting a copy of the [*EncodeableDictionary*](../components/opc-ua/src/Microsoft.Azure.IIoT.OpcUa.Protocol/src/Stack/Encoders/Models/EncodeableDictionary.cs) class and registering it in the *ServiceMessageContext*. Then, the *JsonDecoderEx* can properly decode the EncodeableDictionary:

```cs
var serviceMessageContext = new ServiceMessageContext();
serviceMessageContext.Factory.AddEncodeableType(typeof(EncodeableDictionary));

using (var stream = new MemoryStream(buffer)) {
    using var decoder = new JsonDecoderEx(stream, serviceMessageContext);
    var data = new EncodeableDictionary();
    data.Decode(decoder);

    ...
```

### Pending Alarm snapshots

The OPC Publisher also supports sending Pending Alarms snapshots when listening for events, as described in the user guide for [configuration of events](./publisher-event-configuration.md). When this feature is enabled, it will listen to all ConditionType derived events and cache all that have has the `Retain` property set to true. It will then periodically generate output with an array of all these retained events. When running against the OPC Foundation's Alarms & Conditions reference server sample the output will look like this:

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
          "Payload": {
            "AlarmConditions": [
              {
                "EventId": "5SPXIwZK2U\u002BFqYJJahmL7A==",
                "EventType": "i=2830",
                "LocalTime": {
                  "Offset": 60,
                  "DaylightSavingInOffset": true
                },
                "Message": "The dialog was activated",
                "ReceiveTime": "2022-12-05T08:50:25.6398546Z",
                "Severity": 100,
                "SourceName": "EastTank",
                "SourceNode": "http://opcfoundation.org/AlarmCondition#s=1%3aColours%2fEastTank",
                "Time": "2022-12-05T08:50:25.6398546Z"
              },
              {
                "EventId": "ixCh7U2YkEW361Wg4gHv8g==",
                "EventType": "i=9764",
                "LocalTime": {
                  "Offset": 60,
                  "DaylightSavingInOffset": true
                },
                "Message": "Alarm created.",
                "ReceiveTime": "2022-12-05T08:50:25.6619305Z",
                "Severity": 100,
                "SourceName": "EastTank",
                "SourceNode": "http://opcfoundation.org/AlarmCondition#s=1%3aColours%2fEastTank",
                "Time": "2022-12-05T08:50:25.6310398Z"
              },
              {
                "EventId": "MRGtsM\u002BTJEyEP2Lh3gNpvQ==",
                "EventType": "i=10060",
                "LocalTime": {
                  "Offset": 60,
                  "DaylightSavingInOffset": true
                },
                "Message": "Alarm created.",
                "ReceiveTime": "2022-12-05T08:50:25.6700818Z",
                "Severity": 100,
                "SourceName": "EastTank",
                "SourceNode": "http://opcfoundation.org/AlarmCondition#s=1%3aColours%2fEastTank",
                "Time": "2022-12-05T08:50:25.6314869Z"
              },
              {
                "EventId": "ZhX7mhYdBUGhzUlV\u002BG\u002BDMg==",
                "EventType": "i=2830",
                "LocalTime": {
                  "Offset": 60,
                  "DaylightSavingInOffset": true
                },
                "Message": "The dialog was activated",
                "ReceiveTime": "2022-12-05T08:50:25.6761535Z",
                "Severity": 100,
                "SourceName": "NorthMotor",
                "SourceNode": "http://opcfoundation.org/AlarmCondition#s=1%3aColours%2fNorthMotor",
                "Time": "2022-12-05T08:50:25.6761535Z"
              },
              {
                "EventId": "6eoJjec2ckyKhoBk5SbVtg==",
                "EventType": "i=9764",
                "LocalTime": {
                  "Offset": 60,
                  "DaylightSavingInOffset": true
                },
                "Message": "Alarm created.",
                "ReceiveTime": "2022-12-05T08:50:25.6787723Z",
                "Severity": 100,
                "SourceName": "NorthMotor",
                "SourceNode": "http://opcfoundation.org/AlarmCondition#s=1%3aColours%2fNorthMotor",
                "Time": "2022-12-05T08:50:25.675634Z"
              }
            ]
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

Pending Alarms sent in Samples mode look as follows:

```json
{
  "body":   {
    "NodeId": "i=2253",
    "EndpointUrl": "opc.tcp://localhost:53780/UA/SampleServer",
    "DisplayName": "PendingAlarms",
    "Value": [
      {
        "EventId": "PEl7MUVIJE6tTaGDOt21bw==",
        "EventType": "i=2830",
        "LocalTime": {
          "Offset": 60,
          "DaylightSavingInOffset": true
        },
        "Message": "The dialog was activated",
        "ReceiveTime": "2022-12-05T10:33:20.182657Z",
        "Severity": 100,
        "SourceName": "EastTank",
        "SourceNode": "http://opcfoundation.org/AlarmCondition#s=1%3aColours%2fEastTank",
        "Time": "2022-12-05T10:33:20.182657Z"
      },
      {
        "EventId": "gYwW3oaH6EuQJs/XTQJrHw==",
        "EventType": "i=9764",
        "LocalTime": {
          "Offset": 60,
          "DaylightSavingInOffset": true
        },
        "Message": "The alarm severity has increased.",
        "ReceiveTime": "2022-12-05T10:33:30.3550587Z",
        "Severity": 300,
        "SourceName": "WestTank",
        "SourceNode": "http://opcfoundation.org/AlarmCondition#s=1%3aMetals%2fWestTank",
        "Time": "2022-12-05T10:33:30.3550517Z"
      },
      {
        "EventId": "ruJUGazMLUqhQ6X8cGVm7A==",
        "EventType": "i=10751",
        "LocalTime": {
          "Offset": 60,
          "DaylightSavingInOffset": true
        },
        "Message": "The alarm severity has increased.",
        "ReceiveTime": "2022-12-05T10:33:29.3460232Z",
        "Severity": 300,
        "SourceName": "SouthMotor",
        "SourceNode": "http://opcfoundation.org/AlarmCondition#s=1%3aMetals%2fSouthMotor",
        "Time": "2022-12-05T10:33:29.3460157Z"
      }
    ]
  },
  "enqueuedTime": "Fri Mar 18 2022 14:04:18 GMT+0100 (Central European Standard Time)",
  "properties": {
    "$$ContentType": "application/x-monitored-item-json-v1",
    "iothub-message-schema": "application/json",
    "$$ContentEncoding": "utf-8"
  }
}
```

Finally, in reversable mode, legacy samples messages will look as follows:

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

Samples mode messages are always JSON encoded.
