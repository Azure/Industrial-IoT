# Detailing the output for OPC UA Events in OPC Publisher

This section describes what the output looks like when listening for events in the OPC Publisher. Events are produced in the PubSub format specified in the OPC UA Standard. The payload is an event which consists of fields selected in the select clause and its values.

Here is an example of the output we get when listening to events from the Simple Events sample:

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
              "Value": {
                "EventId": "+6CQjN1eqkO6+yHJnxMz5w==",
                "EventType": "http://microsoft.com/Opc/OpcPlc/SimpleEvents#i=14",
                "Message": "The system cycle '59' has started.",
                "ReceiveTime": "2021-06-21T12:38:55.5814091Z",
                "Severity": 1,
                "SourceName": "System",
                "SourceNode": "i=2253",
                "Time": "2021-06-21T12:38:55.5814078Z"
              },
              "SourceTimestamp": "2021-06-21T12:38:55.5814091Z"
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

A few things to note here. Here the event in the payload is named "SimpleEvents" which has been configured to by setting the DisplayName property of the events configuration. Also note that all fields+values reside under the Value key.

The format produced here does not contain enough information to decode the JSON properly. If you need this there is a command-line option in the OPC Publisher called "UseReversibleEncoding", which can be set to either true or false. The default value is false. If you enable this setting the output will look like this:

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
              },
              "SourceTimestamp": "2021-06-21T12:45:22.5819817Z"
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

The OPC Publisher also support the Pending Alarms view when listening for events, as described in the user guide for configuration of events. When this feature is enabled, it will listen to all ConditionType derived events and cache all of them that has the Retain property set to true. It will then periodically generate output with an array of all these cached events. When running against the Alarms & Conditions sample the output will look like this:

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
                "ConditionId": "http://microsoft.com/Opc/OpcPlc/AlarmsInstance#s=1%3aMetals%2fSouthMotor%3fGold",
                "ConfirmedState": "Unconfirmed",
                "EnabledState": "Enabled",
                "EventId": "BaZCnQgtw02nSBhcYsYf6w==",
                "EventType": "i=9764",
                "Message": "The alarm is active.",
                "ReceiveTime": "2021-06-21T12:56:44.9769512Z",
                "Retain": true,
                "Severity": 100,
                "SourceName": "SouthMotor",
                "SourceNode": "http://microsoft.com/Opc/OpcPlc/AlarmsInstance#s=1%3aMetals%2fSouthMotor"
              },
              {
                "ConditionId": "http://microsoft.com/Opc/OpcPlc/AlarmsInstance#s=1%3aColours%2fNorthMotor%3fYellow",
                "ConditionName": "Yellow",
                "ConfirmedState": "Unconfirmed",
                "EnabledState": "Enabled",
                "EventId": "2nHjlUsDok61RU2B40J71A==",
                "EventType": "i=10060",
                "Message": "The alarm is active.",
                "ReceiveTime": "2021-06-21T12:56:44.9793124Z",
                "Retain": true,
                "Severity": 100,
                "SourceName": "NorthMotor",
                "SourceNode": "http://microsoft.com/Opc/OpcPlc/AlarmsInstance#s=1%3aColours%2fNorthMotor"
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

The key thing to highlight here is that the payload is an array of events, which has the Retain property set to true. Otherwise it's very similar to the regular events output.
