# Use configuration files for OPC UA Events in OPC Publisher

This section describes how to configure the OPC Publisher to listen to events. Primarily you have to configure three things:
* The source node you want to receive events for.
* A select clause specifying which fields that should be in the event.
* A where clause specifying which events to receive.

 OPC Publisher supports two types of configurations to specify that:
* Normal, or advanced, configuration mode where you explicitly specify the select and where clauses.
* Simple configuration mode, where you specify the source node and the event type you want to filter on and then the OPC Publisher constructs the select and where clauses for you.

In the configuration file you can specify how many event configurations as you like and you can also combine events and data nodes for a single endpoint.

Here is an example of a configuration file in normal or advanced mode:
```json
[
  {
    "EndpointUrl": "opc.tcp://testserver:62563/Quickstarts/SimpleEventsServer",
    "OpcEvents": [
      {
        "Id": "i=2253",
        "DisplayName": "SimpleEventServerEvents",
        "SelectClauses": [
          {
            "TypeDefinitionId": "i=2041",
            "BrowsePath": [
              "EventId"
            ]
          },
          {
            "TypeDefinitionId": "i=2041",
            "BrowsePath": [
              "Message"
            ]
          },
          {
            "TypeDefinitionId": "ns=2;i=235",
            "BrowsePath": [
              "/2:CycleId"
            ]
          },
          {
            "TypeDefinitionId": "nsu=http://opcfoundation.org/Quickstarts/SimpleEvents;i=235",
            "BrowsePath": [
              "/http://opcfoundation.org/Quickstarts/SimpleEvents#CurrentStep"
            ]
          }
        ],
        "WhereClause": {
            "Elements": [
                "FilterOperator": "OfType",
                "FilterOperands": [
                  {
                    "Value": "ns=2;i=235"
                  }
                ]
            ]
          }
      }
    ]
  }
]
```
As highlighted in the example above you can specify namespaces both by using the index or the full name for the namespace. Also look at how the BrowsePath can be configured.

And here is an example of a configuration file in simple mode:
```json
[
  {
    "EndpointUrl": "opc.tcp://testserver:62563/Quickstarts/SimpleEventsServer",
    "OpcEvents": [
      {
        "Id": "i=2253",
        "DisplayName": "SimpleEventServerEvents",
        "TypeDefinitionId": "ns=2;i=235" 
      }
    ]
  }
]
```

When you specify a simple mode configuration, the OPC Publisher does two things:
* It looks at the TypeDefinitionId and traverses the inheritance tree for that event type, collecting all the fields. Then it constructs a select clause with all the fields it finds.
* It creates a where clause that is OfType(TypeDefinitionId).

In addition to this, you can also configure to enable pending alarms view. What this does is that it listens to ConditionType derived events, record unique occurrences of them and on periodic updates will send a message containing all the unique events that has the Retain property set to True. This enables you to get a snapshot view of all pending, or active, alarms and conditions which can be very useful for dashboard-like scenarios.

Here is an example of a configuration for a pending alarms:
```json
[
  {
    "EndpointUrl": "opc.tcp://testserver:62563/Quickstarts/AlarmConditionServer",
    "OpcEvents": [
      {
        "DisplayName": "AlarmConditions",
        "Id": "i=2253",
        "TypeDefinitionId": "i=2915",
        "PendingAlarms": {
          "IsEnabled": true,
          "UpdateInterval": 10,
          "SnapshotInterval": 20,
          "CompressedPayload": false
        }
      }
    ]
  }
]
```

The PendingAlarms section consists of the following properties:
* IsEnabled - defines if pending alarms view is enabled or not. If disabled it will work as normal events.
* UpdateInterval - the interval, in seconds, which a message is sent if anything has been updated during this interval.
* SnapshotInterval - the interval, in seconds, that triggers a message to be sent regardless of if there has been an update or not.
* CompressedPayload - Should the message be compressed using GZip? Since we are accumulating all retained messages we might be sending a large message, so compression can be used to overcome the limit of a single IoTHub message.

You can use the pending alarm configuration regardless if you are using normal or simple mode.