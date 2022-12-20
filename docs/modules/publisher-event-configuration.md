# Use configuration files for OPC UA Events in OPC Publisher

This section describes how to configure the OPC Publisher to listen to events using an event filter.

OPC Publisher supports two types of event filter configurations you can specify:

* [Simple event filter](#simple-event-filter) configuration mode, where you specify the source node and the event type you want to filter on and then the OPC Publisher constructs the select and where clauses for you.
* [Advanced event filter](#advanced-event-filter) configuration mode where you explicitly specify the select and where clauses.

In the configuration file you can specify how many event configurations as you like and you can also combine events and data nodes for a single endpoint.

In addition you can configure optional [pending alarms](#pending-alarms-handling-options) reporting where OPC Publisher reports pending alarms at a configured time interval.

## Simple event filter

As highlighted in the example above you can specify namespaces both by using the index or the full name for the namespace. Also look at how the BrowsePath can be configured.

Here is an example of a configuration file in simple mode:

```json
[
    {
        "EndpointUrl": "opc.tcp://testserver:62563/Quickstarts/SimpleEventsServer",
        "OpcNodes": [
            {
                "Id": "i=2253",
                "DisplayName": "SimpleEventServerEvents",
                "EventFilter": {
                    "TypeDefinitionId": "ns=2;i=235"
                }
            }
        ]
    }
]
```

To subscribe to an event you specify the source node (in this case the server node which has node id `i=2253`) and the event type to monitor (in this case `ns=2;i=235`).
When you specify a simple mode configuration, the OPC Publisher does two things:

* It looks at the TypeDefinitionId of the event type to monitor and traverses the inheritance tree for that event type, collecting all fields. Then it constructs a select clause with all the fields it finds.
* It creates a where clause that is OfType(TypeDefinitionId) to filter the events to just the selected event type.

## Advanced event filter

To configure an advanced event filter you have to specify a full event filter which at minimum consists of three things:

* The source node you want to receive events for (in the example below again the server node which has node id `i=2253`).
* A select clause specifying which fields should be in the reported event. This can include a data set class field id that is then used as identifier in the dataset metadata for the dataset class.
* A where clause specifying the filter AST.

Here is an example of a configuration file that selects events using an advanced event filter:

```json
[
    {
        "EndpointUrl": "opc.tcp://testserver:62563/Quickstarts/SimpleEventsServer",
        "OpcNodes": [
            {
                "Id": "i=2253",
                "DisplayName": "SimpleEventServerEvents",
                "EventFilter": {
                    "SelectClauses": [
                        {
                            "TypeDefinitionId": "i=2041",
                            "DataSetClassFieldId ": "D3EB3722-E956-4E5E-925B-FB727B737520",
                            "BrowsePath": [
                                "EventId"
                            ]
                        },
                        {
                            "TypeDefinitionId": "i=2041",
                            "DataSetClassFieldId ": "A435F616-CE1E-4FBD-A819-03175EB49231",
                            "BrowsePath": [
                                "Message"
                            ]
                        },
                        {
                            "TypeDefinitionId": "ns=2;i=235",
                            "DataSetClassFieldId ": "BD236A98-8DA3-40A1-B8E8-00AB23A6B5E9",
                            "BrowsePath": [
                                "/2:CycleId"
                            ]
                        },
                        {
                            "TypeDefinitionId": "nsu=http://opcfoundation.org/Quickstarts/SimpleEvents;i=235",
                            "DataSetClassFieldId ": "9F9A420B-509E-488B-A7A4-F320F8223E9E",
                            "BrowsePath": [
                                "/http://opcfoundation.org/Quickstarts/SimpleEvents#CurrentStep"
                            ]
                        }
                    ],
                    "WhereClause": {
                        "Elements": [
                            {
                                "FilterOperator": "OfType",
                                "FilterOperands": [
                                    {
                                        "Value": "ns=2;i=235"
                                    }
                                ]
                            }
                        ]
                    }
                }
            }
        ]
    }
]
```

The exact syntax allowed can be found in the OPC UA reference documentation.  Note that not all servers support all filter capabilities.  You can trouble shoot issues using the OPC Publisher logs.

## Pending alarms handling options

In addition to this, you can also configure to enable pending alarms view. What this does is that it listens to ConditionType derived events, records unique occurrences of them and on periodic updates will send a message containing all the unique events that has the Retain property set to True. This enables you to get a snapshot view of all pending, or active, alarms and conditions which can be very useful for dashboard-like scenarios.

Here is an example of a configuration for a pending alarms:

```json
[
    {
        "EndpointUrl": "opc.tcp://testserver:62563/Quickstarts/AlarmConditionServer",
        "OpcEvents": [
            {
                "DisplayName": "AlarmConditions",
                "Id": "i=2253",
                "EventFilter": {
                    "TypeDefinitionId": "i=2915",
                    "PendingAlarms": {
                        "IsEnabled": true,
                        "UpdateInterval": 10,
                        "SnapshotInterval": 20
                    }
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

You can use the pending alarm configuration regardless if you are using advanced or simple event filters.
