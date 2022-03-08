[Home](../../readme.md)

# Configuration via IoT Hub Direct methods

OPC Publisher version 2.8.2 implements [IoT Hub Direct Methods](https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-direct-methods), which can be called from an application using the [IoT Hub Device SDK](https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-sdks).

The following direct methods are exposed:

- PublishNodes_V1
- UnpublishNodes_V1
- UnpublishAllNodes_V1
- GetConfiguredEndpoints_V1
- GetConfiguredNodesOnEndpoint_V1
- GetDiagnosticInfo_V1
- AddOrUpdateEndpoints_V1

If you need to migrate your application from OPC Publisher 2.5.x to OPC Publisher 2.8.2, we provide the needed information in a [separate document](./publisher-migrationpath.md).

## Terminology

The definitions of the important terms used are described below:

- __DataSet__ is a group of nodes within one OPC UA Server publishing of data value changes of those nodes is done with the same publishing interval.
- __DataSetWriter__ has one DataSet and contains all information to establish a connection to an OPC UA Server.
- __DatSetWriterGroup__ is used to group several DataSetWriter's for a specific OPC UA server.

## Payload Schema

The `_V1` direct methods use the payload schema as described below:

```json
{
  "EndpointUrl": "string",
  "UseSecurity": "boolean",
  "OpcAuthenticationMode": "string",
  "UserName": "string",
  "Password": "string",
  "DataSetWriterGroup": "string",
  "DataSetWriterId": "string",
  "DataSetPublishingInterval": "integer",
  "DataSetPublishingIntervalTimespan": "string",
  "Tag": "string",
  "OpcNodes":
  [
    {
      "Id": "string",
      "ExpandedNodeId": "string",
      "OpcSamplingInterval": "integer",
      "OpcSamplingIntervalTimespan": "string",
      "OpcPublishingInterval": "integer",
      "OpcPublishingIntervalTimespan": "string",
      "DataSetFieldId ": "string",
      "DisplayName": "string",
      "HeartbeatInterval": "integer",
      "HeartbeatIntervalTimespan": "string",
      "QueueSize": "integer"
    }
  ]
}
```

Method call's request attributes are as follows:

| Attribute                           | Mandatory | Type            | Default                     | Description                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
|-------------------------------------|-----------|-----------------|-----------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `EndpointUrl`                       | Yes       | String          | N/A                         | The OPC UA server endpoint URL                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             |
| `UseSecurity`                       | No        | Boolean         | `false`                     | Controls whether to use a secure OPC UA mode to establish a session to the OPC UA server endpoint                                                                                                                                                                                                                                                                                                                                                                                                                                                                          |
| `OpcAuthenticationMode`             | No        | Enum            | `Anonymous`                 | Enum to specify the session authentication. <br>Options: Anonymous, UserName                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               |
| `UserName`                          | No        | String          | `null`                      | The username for the session authentication <br>Mandatory if OpcAuthentication mode is UserName                                                                                                                                                                                                                                                                                                                                                                                                                                                                            |
| `Password`                          | No        | String          | `null`                      | The password for the session authentication <br>Mandatory if OpcAuthentication mode is UserName                                                                                                                                                                                                                                                                                                                                                                                                                                                                            |
| `DataSetWriterGroup`                | No        | String          | `EndpointUrl`               | The data set writer group collecting datasets defined for a certain <br>endpoint uniquely identified by the above attributes. <br>This attribute is used to identify the session opened into the <br>server. The default value consists of the EndpointUrl string, <br>followed by a deterministic hash composed of the <br>EndpointUrl, UseSecurity, OpcAuthenticationMode, UserName and Password attributes.                                                                                                                                                             |
| `DataSetWriterId`                   | No        | String          | `DataSetPublishingInterval` | The unique identifier for a data set writer used to collect <br>OPC UA nodes to be semantically grouped and published with <br>the same publishing interval. <br>When not specified a string representing the common <br>publishing interval of the nodes in the data set collection. <br>This attribute uniquely identifies a data set <br>within a DataSetWriterGroup. The uniqueness is determined <br>using the provided DataSetWriterId and the publishing <br>interval of the grouped OpcNodes.  An individual <br>subscription is created for each DataSetWriterId. |
| `DataSetPublishingInterval`         | No        | Integer         | `null`                      | The publishing interval used for a grouped set of nodes under a certain DataSetWriter. <br>Value expressed in milliseconds. <br>Ignored when `DataSetPublishingIntervalTimespan` is present. <br> _Note_: When a specific node underneath DataSetWriter defines `OpcPublishingInterval` (or Timespan), <br>its value will overwrite publishing interval for the specified node.                                                                                                                                                                                            |
| `DataSetPublishingIntervalTimespan` | No        | String          | `null`                      | The publishing interval used for a grouped set of nodes under a certain DataSetWriter. <br>Value expressed as a Timespan string ({d.hh:mm:dd.fff}). <br>When both Intervals are specified, the Timespan will win and be used for the configuration. <br> _Note_: When a specific node underneath DataSetWriter defines `OpcPublishingInterval` (or Timespan), <br>its value will overwrite publishing interval for the specified node.                                                                                                                                     |
| `Tag`                               | No        | String          | empty                       | TODO                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       |
| `OpcNodes`                          | No        | List\<OpcNode\> | empty                       | The DataSet collection grouping the nodes to be published for <br>the specific DataSetWriter defined above.                                                                                                                                                                                                                                                                                                                                                                                                                                                                |

_Note_: `OpcNodes` field is mandatory for `PublishNodes_V1`. It is optional for `UnpublishNodes_V1` and `AddOrUpdateEndpoints_V1`. And `OpcNodes` field shouldn't be specified for the rest of the direct methods.

OpcNode attributes are as follows:

| Attribute                       | Mandatory | Type    | Default | Description                                                                                                                                                                                                                                                                                                                                |
|---------------------------------|-----------|---------|---------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Id`                            | Yes*      | String  | N/A     | The OPC UA NodeId in the OPC UA server whose data value changes should be published. <br>Can be specified as NodeId or ExpandedNodeId as per OPC UA specification, <br>or as ExpandedNodeId IIoT format {NamespaceUi}#{NodeIdentifier}. <br>*_Note_: `Id` field may be omitted when `ExpandedNodeId` is present.                           |
| `ExpandedNodeId`                | No        | String  | null    | Enables backwards compatibility. <br>Must be specified as ExpandedNodeId as per OPC UA specification. <br>*_Note_: when `ExpandedNodeId` is present `Id` field may be omitted.                                                                                                                                                             |
| `OpcSamplingInterval`           | No        | Integer | 1000    | The sampling interval for the monitored item to be <br>published. Value expressed in milliseconds. The value is used as defined in the OPC UA specification. Ignored when OpcSamplingIntervalTimespan is present.                                                                                                                          |
| `OpcSamplingIntervalTimespan`   | No        | String  | null    | The sampling interval for the monitored item to be <br>published. Value expressed in Timespan <br>string({d.hh:mm:dd.fff}). <br>The value is used as defined in the OPC UA specification.                                                                                                                                                  |
| `OpcPublishingInterval`         | No        | Integer | 1000    | The publishing interval for the monitored item to be published. <br>Value expressed in milliseconds. <br>This value will overwrite the publishing interval defined in the DataSetWriter for the specified node. <br>The value is used as defined in the OPC UA specification. <br>Ignored when `OpcPublishingIntervalTimespan` is present. |
| `OpcPublishingIntervalTimespan` | No        | String  | null    | The publishing interval for the monitored item to be published. <br>Value expressed in Timespan string({d.hh:mm:dd.fff}). <br>This value will overwrite the publishing interval defined in the DataSetWriter for the specified node. <br>The value is used as defined in the OPC UA specification.                                         |
| `DataSetFieldId`                | No        | String  | null    | A user defined tag used to identify the Field in the <br>DataSet telemetry message when publisher runs in <br>PubSub message mode.                                                                                                                                                                                                         |
| `DisplayName`                   | No        | String  | null    | A user defined tag to be added to the telemetry message <br>when publisher runs in Samples message mode.                                                                                                                                                                                                                                   |
| `HeartbeatInterval`             | No        | Integer | 0       | The interval used for the node to publish a value (a publisher <br>cached one) even if the value hasn't been <br>changed at the source. This value is represented in seconds. <br>0 means the heartbeat mechanism is disabled. <br>This value is ignored when HeartbeatIntervalTimespan is present                                         |
| `HeartbeatIntervalTimespan`     | No        | String  | null    | The interval used for the node to publish a value (a publisher <br>cached one) even if the value hasn't been <br>changed at the source. This value is represented in seconds. <br>Value expressed in Timespan string({d.hh:mm:dd.fff}).                                                                                                    |
| `QueueSize`                     | No        | Integer | 1       | The desired QueueSize for the monitored item to be published.                                                                                                                                                                                                                                                                              |

Now let's dive into each direct method request and response payloads with examples.

## Direct methods

### PublishNodes_V1

PublishNodes enables a client to add a set of nodes to be published for a specific [`DataSetWriter`](publisher-directmethods.md#terminologies). When a `DataSetWriter` already exists, the nodes are incrementally added to the same [`dataset`](publisher-directmethods.md#terminologies). When it doesn't already exist, a new `DataSetWriter` is created with the initial set of nodes contained in the request.

  _Request_: follows strictly the request [payload schema](publisher-directmethods.md#payload-schema), the `OpcNodes` attribute being mandatory.

  _Response_: when successful Status 200 and an empty json (`{}`) as payload

  _Exceptions_: an exception is thrown when method call returns status other than 200

  _Example_:

  > _Method Name_: `PublishNodes_V1`
  >
  > _Request_:
  >
  > ```json
  > {
  >    "EndpointUrl": "opc.tcp://opcplc:50000",
  >    "DataSetWriterGroup": "Asset0",
  >    "DataSetWriterId": "DataFlow0",
  >    "DataSetPublishingInterval": 5000,
  >    "OpcNodes": [
  >       {
  >          "Id": "nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt0"
  >       }
  >    ]
  > }
  > ```
  >
  > _Response_:
  >
  > ```json
  > {
  >    "status": 200,
  >    "payload": {}
  > }
  > ```

### UnpublishNodes_V1

UnpublishNodes method enables a client to remove nodes from a previously configured DataSetWriter.
If value of the `OpcNodes` attribute is `null` or an empty list, then the whole DataSetWriter entity is removed.

_Note_: If all the nodes from a DataSet are to be unpublished, the DataSetWriter entity is removed from the configuration storage.

  _Request_:  follows strictly the request payload schema, the `OpcNodes` attribute not being mandatory.

  _Response_: when successful - Status 200 and an empty json (`{}`) as Payload

  _Exceptions_: a response corresponding to an exception will be returned if:

  - request payload contains an endpoint (DataSet) that isn't present in publisher configuration

  - request payload contains a node that isn't present in publisher configuration

  _Example_:

  > _Method Name_: `UnpublishNodes_V1`
  >
  > _Request_:
  >
  > ```json
  > {
  >    "EndpointUrl": "opc.tcp://opcplc:50000",
  >    "DataSetWriterGroup": "Asset0",
  >    "DataSetWriterId": "DataFlow0",
  >    "DataSetPublishingInterval": 5000,
  >    "OpcNodes": [
  >       {
  >          "Id": "nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt0"
  >       }
  >    ]
  > }
  > ```
  >
  > _Response_:
  >
  > ```json
  > {
  >    "status": 200,
  >    "payload": {}
  > }
  > ```

### UnpublishAllNodes_V1

UnpublishAllNodes method enables a client to remove all the nodes from a previously configured DataSetWriter.
The DataSetWriter entity will be removed from the configuration storage.
When an empty payload is set or the endpoint in payload is null, the complete configuration of the publisher will be purged.

  _Request_: follows strictly the request payload schema, the `OpcNodes` attribute should be excluded.

  _Response_: when successful - Status 200 and an empty json (`{}`) as Payload

  _Exceptions_: a response corresponding to an exception will be returned if:

  - request payload contains an endpoint (DataSet) that isn't present in publisher configuration

  - request payload contains `OpcNodes`

  _Example_:

  > _Method Name_: `UnpublishAllNodes_V1`
  >
  > _Payload_:
  >
  > ```json
  > {
  >    "EndpointUrl": "opc.tcp://opcplc:50000",
  >    "DataSetWriterGroup": "Asset1",
  >    "DataSetWriterId": "DataFlow1",
  >    "DataSetPublishingInterval": 5000
  > }
  > ```
  >
  > _Response_:
  >
  > ```json
  > {
  >    "status": 200,
  >    "payload": {}
  > }
  > ```

### GetConfiguredEndpoints_V1

Returns the configured endpoints (Datasets)

  _Request_: empty json (`{}`)

  _Response_: the list of configured endpoints (and optional parameters).

  _Exceptions_: an exception is thrown when method call returns status other than 200

  _Example_:

  > _Method Name_: `GetConfiguredEndpoints_V1`
  >
  > _Response_:
  >
  > ```json
  > {
  >    "status": 200,
  >    "payload": {
  >       "endpoints": [
  >          {
  >             "endpointUrl": "opc.tcp://opcplc:50000",
  >             "dataSetWriterGroup": "Asset1",
  >             "dataSetWriterId": "DataFlow1",
  >             "dataSetPublishingInterval": 5000
  >           },
  >           {
  >              "endpointUrl": "opc.tcp://opcplc:50001"
  >           }
  >       ]
  >    }
  > }
  > ```

### GetConfiguredNodesOnEndpoint_V1

Returns the nodes configured for one Endpoint (Dataset)

  _Request_: contains the elements necessary to uniquely identify a Dataset. The EndpointUrl is mandatory in the request, the other attributes are optional and can be used to refine your result.

  _Response_: list of `OpcNodes` configured for the selected Endpoint (and optional parameters).

  _Exceptions_: an exception is thrown when method call returns status other than 200

  _Example_:

  > _Method Name_: `GetConfiguredNodesOnEndpoints_V1`
  >
  > _Payload_:
  >
  > ```json
  > {
  >    "EndpointUrl": "opc.tcp://192.168.100.20:50000"
  > }
  > ```
  >
  > _Response_:
  >
  > ```json
  > {
  >    "status": 200,
  >    "payload": {
  >       "opcNodes": [
  >          {
  >             "id": "nsu=http://microsoft.com/Opc/OpcPlc/;s=SlowUInt1",
  >             "opcSamplingIntervalTimespan": "00:00:10",
  >             "heartbeatIntervalTimespan": "00:00:50"
  >           }
  >       ]
  >    }
  > }
  > ```

### GetDiagnosticInfo_V1

Returns a list of actual metrics for every endpoint (Dataset).

  _Request_: empty json (`{}`)

  _Response_: list of actual metrics for every endpoint (Dataset).

  _Exceptions_: an exception is thrown when method call returns status other than 200

  _Note_: passwords aren't being part of the response.

  _Example_:

  > _Method Name_: `GetDiagnosticInfo_V1`
  >
  > _Response_:
  >
  > ```json
  > {
  >    "status":200,
  >    "payload":[
  >       {
  >          "endpoint": {
  >             "endpointUrl": "opc.tcp://opcplc:50000",
  >             "dataSetWriterGroup": "Asset1",
  >             "useSecurity": false,
  >             "opcAuthenticationMode": "UsernamePassword",
  >             "opcAuthenticationUsername": "Usr"
  >          },
  >          "sentMessagesPerSec": 2.6,
  >          "ingestionDuration": "{00:00:25.5491702}",
  >          "ingressDataChanges": 25,
  >          "ingressValueChanges": 103,
  >          "ingressBatchBlockBufferSize": 0,
  >          "encodingBlockInputSize": 0,
  >          "encodingBlockOutputSize": 0,
  >          "encoderNotificationsProcessed": 83,
  >          "encoderNotificationsDropped": 0,
  >          "encoderIoTMessagesProcessed": 2,
  >          "encoderAvgNotificationsMessage": 41.5,
  >          "encoderAvgIoTMessageBodySize": 6128,
  >          "encoderAvgIoTChunkUsage": 1.158034,
  >          "estimatedIoTChunksPerDay": 13526.858105160689,
  >          "outgressBatchBlockBufferSize": 0,
  >          "outgressInputBufferCount": 0,
  >          "outgressInputBufferDropped": 0,
  >          "outgressIoTMessageCount": 0,
  >          "connectionRetries": 0,
  >          "opcEndpointConnected": true,
  >          "monitoredOpcNodesSucceededCount": 5,
  >          "monitoredOpcNodesFailedCount": 0
  >       }
  >    ]
  > }
  > ```

### AddOrUpdateEndpoints_V1

This method performs a complete `published_nodes.json` update and an update of multiple
endpoints (DataSets) at once. Unlike `PublishNodes_V1` method, `AddOrUpdateEndpoints_V1`
changes the complete node set for an endpoint (DataSet) with the one provided in the method's request payload.
By providing an empty list of nodes in the request, the user can remove the
previously configured nodes for a specific endpoint (DataSet).

  _Request_: represents a list of objects, which should strictly follow the request payload schema as
  described above. The `OpcNodes` attribute being empty list or `null` will be interpreted as a removal
  request for that endpoint (DataSet).

  _Response_: when successful - Status 200 and an empty json (`{}`) as payload

  _Exceptions_: a response corresponding to an exception will be returned if:

  - request payload contains deletion request for an endpoint (DataSet) that isn't present in publisher configuration

  - request payload contains two or more entries for the same endpoint (DataSet)

  _Example_:
  > _Method Name_: `AddOrUpdateEndpoints_V1`
  >
  > _Payload_:
  >
  > ```json
  > [
  >    {
  >       "EndpointUrl": "opc.tcp://opcplc:50000",
  >       "DataSetWriterGroup": "Asset1",
  >       "DataSetWriterId": "DataFlow1",
  >       "DataSetPublishingInterval": 5000,
  >       "OpcNodes": [
  >          {
  >             "Id": "nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt0",
  >          }
  >       ]
  >    },
  >    {
  >       "EndpointUrl": "opc.tcp://opcplc:50001",
  >       "DataSetWriterGroup": "Asset2",
  >       "DataSetWriterId": "DataFlow2",
  >       "OpcNodes": []
  >    }
  > ]
  > ```
  >
  > _Response_:
  >
  > ```json
  > {
  >    "status": 200,
  >    "payload": {}
  > }
  > ```
