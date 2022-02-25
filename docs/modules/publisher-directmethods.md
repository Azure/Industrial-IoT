[Home](../../readme.md)

# Configuration via IoT Hub Direct methods

OPC Publisher version 2.8.2 implements the following [IoT Hub Direct Methods](https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-direct-methods) which can be called from an application (anywhere in the world) leveraging the [IoT Hub Device SDK](https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-sdks).

There are some direct methods which are inherited from 2.5.x in addition to new ones.

Inherited direct methods from 2.5.x:

- PublishNodes_V1
- UnpublishNodes_V1
- UnpublishAllNodes_V1
- GetConfiguredEndpoints_V1
- GetConfiguredNodesOnEndpoint_V1
- GetDiagnosticInfo_V1

New direct methods:

- AddOrUpdateEndpoints_V1

## Terminologies

The definitions of the important terms used are described below:

- __DataSet__ is a group of NodeIds within an OPC UA Server to be published with the same publishing interval.
- __DataSetWriter__ has one DataSet and contains the elements required to successfully establish a connection to the OPC UA Server.
- __DatSetWriterGroup__ is used to group several DataSetWriters for a specific OPC UA server.

## Payload Schema

The `_V1` direct methods  uses the  payload schema as described below:

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
      "ExpandedNodeIdId": "string",
      "OpcSamplingInterval": "integer",
      "OpcSamplingIntervalTimespan": "string",
      "OpcPublishingInterval": "integer",
      "OpcPublishingIntervalTimespan": "string",
      "DataSetFieldId ": "string",
      "DisplayName": "string",
      "HeartbeatInterval": "integer",
      "HeartbeatIntervalTimespan": "string",
      "SkipFirst": "boolean",
      "QueueSize": "integer"
    }
  ]
}
```

Method call's request attributes are as follows:

| Attribute                   | Mandatory | Type    | Default                   | Description                                                  |
| --------------------------- | --------- | ------- | ------------------------- | ------------------------------------------------------------ |
| `EndpointUrl`               | yes       | String  | N/A                       | The OPC UA Server’s endpoint URL                             |
| `UseSecurity`               | no        | Boolean | false                     | Desired opc session security mode                            |
| `OpcAuthenticationMode`     | no        | Enum    | Anonymous                 | Enum to specify the session authentication.<br>Options: Anonymous, UserName |
| `UserName`                  | no        | String  | null                      | The username for the session authentication<br>Mandatory if OpcAuthentication mode is UserName |
| `Password`                  | no        | String  | null                      | The password for the session authentication<br>Mandatory if OpcAuthentication mode is UserName |
| `DataSetWriterGroup`        | no        | String  | EndpointUrl               | The writer group collecting datasets defined for a certain <br>endpoint uniquely identified by the above attributes. <br>This is used to identify the session opened into the <br>server. The default value consists of the EndpointUrl string, <br>followed by a deterministic hash composed of the <br>EndpointUrl, security and authentication attributes. |
| `DataSetWriterId`           | no        | String  | DataSetPublishingInterval | The unique identifier for a data set writer used to collect <br>opc nodes to be semantically grouped and published with <br>a same publishing interval. <br>When not specified a string representing the common <br>publishing interval of the nodes in the data set collection. <br>This the DataSetWriterId  uniquely identifies a data set <br>within a DataSetGroup. The unicity is determined <br>using the provided DataSetWriterId and the publishing <br>interval of the grouped OpcNodes.  An individual <br>subscription is created for each DataSetWriterId |
| `DataSetPublishingInterval` | no        | Integer | false                     | The publishing interval used for a grouped set of nodes <br>under a certain DataSetWriter. When defined it <br>overrides the OpcPublishingInterval value in the OpcNodes <br>if grouped underneath a DataSetWriter. |
| `Tag`                       | no        | String  | empty                     | TODO                                                         |
| `OpcNodes`                  | yes       | OpcNode | empty                     | The DataSet collection grouping the nodes to be published for <br>the specific DataSetWriter defined above. |

_Note_: __OpcNodes__ field is mandatory for PublishNodes_V1, UnpublishNodes_V1 and AddOrUpdateEndpoints_V1. It should not be specified for the rest of the direct methods.

OpcNode attributes are as follows:

| Attribute                       | Mandatory | Type    | Default | Description                                                  |
| ------------------------------- | --------- | ------- | ------- | ------------------------------------------------------------ |
| `Id`                            | Yes       | String  | N/A     | The node Id to be published in the opc ua server. <br>Can be specified as NodeId or Expanded NodeId <br>in as per opc ua spec, or as ExpandedNodeId IIoT format <br>{NamespaceUi}#{NodeIdentifier}. |
| `ExpandedNodeIdId`              | No        | String  | null    | Backwards compatibility form for Id attribute. Must be <br>specified as expanded node Id as per OPC UA Spec. |
| `OpcSamplingInterval`           | No        | Integer | 1000    | The sampling interval for the monitored item to be <br>published. Value expressed in milliseconds. |
| `OpcSamplingIntervalTimespan`   | No        | String  | null    | The sampling interval for the monitored item to be <br>published. Value expressed in Timespan <br>string({d.hh:mm:dd.fff}). <br>Ignored when OpcSamplingInterval is present. |
| `OpcPublishingInterval`         | No        | Integer | 1000    | The publishing interval for the monitored item to be <br>published. Value expressed in milliseconds. <br>This value will be overwritten when a publishing interval <br>is explicitly defined in the DataSetWriter owning this OpcNode. |
| `OpcPublishingIntervalTimespan` | No        | String  | null    | The publishing interval for the monitored item to be <br>published. Value expressed in Timespan <br>string({d.hh:mm:dd.fff}). <br>This value will be overwritten when a publishing interval <br>is explicitly defined in the DataSetWriter owning this OpcNode. <br>Ignored when OpcPublishingInterval is present |
| `DataSetFieldId`                | No        | String  | null    | A user defined tag used to identify the Field in the <br>DataSet telemetry message when publisher runs in <br>PubSub message mode. |
| `DisplayName`                   | No        | String  | null    | A user defined tag to be added to the telemetry message <br>when publisher runs in Samples message mode. |
| `HeartbeatInterval`             | No        | Integer | 0       | The interval used for the node to publish a value (a publisher <br>cached one) even if the value has not been <br>changed at the source. This value is represented in seconds. <br>0 means the heartbeat mechanism is disabled. <br>This value is ignored when HeartbeatIntervalTimespan is present |
| `HeartbeatIntervalTimespan`     | No        | String  | null    | The interval used for the node to publish a value (a publisher <br>cached one) even if the value has not been <br>changed at the source. This value is represented in seconds. <br>Value expressed in Timespan string({d.hh:mm:dd.fff}). |
| `SkipFirst`                     | No        | Boolean | false   | Instructs the publisher not to add to telemetry the<br> Initial DataChange (after subscription activation) for this OpcNode. |
| `QueueSize`                     | No        | Integer | 1       | The desired QueueSize for the monitored item to be published. |

_Note_: __Id__ field may be omitted when ExpandedNodeIdId is present.

Now let's dive into each direct method request and response payloads with examples.

__TODO__: Update the responses in 2.8.2 after backwards compatibility fixes.

## PublishNodes_V1

PublishNodes enables a client to add a set of nodes to be published for a specific [`DataSetWriter`](publisher-directmethods.md#terminologies). When a `DataSetWriter` already exists, the nodes are incrementally added to the very same [`dataset`](publisher-directmethods.md#terminologies). When it does not already exist, a new `DataSetWriter` is created with the initial set of nodes contained in the request.

  _Request_: follows strictly the request [payload schema](publisher-directmethods.md#payload-schema), the OpcNodes attribute being mandatory.

  _Response_: when successful Status 200 and an empty json (`{}`) as payload

  _Exceptions_: an exception is thrown when method call returns status other than 200

  _Example_:

  > _Method Name_: `PublishNodes_V1`
  >
  > _Request_:
  >
  > ```json
  > {
  >   "EndpointUrl":"opc.tcp://opcplc:50000/",
  >   "DataSetWriterGroup":"Asset0",
  >   "DataSetWriterId":"DataFlow0",
  >   "DataSetPublishingInterval":5000,
  >   "OpcNodes":[
  >     {
  >       "Id":"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt0"
  >     }
  >   ]
  > }
  > ```
  >
  > _Response_:
  >
  > ```json
  > {
  >   "status":200,
  >   "payload":{
  >   }
  > }
  > ```

## UnpublishNodes_V1

UnpublishNodes method enables a client to remove nodes from a previously configured DataSetWriter.
If value of OpcNodes attribute is `null` or empty list then the whole DataSetWriter entity is completely removed.

_Note_: If all the nodes from a DataSet are to be unpublished, the DataSetWriter entity is completely removed from the configuration storage.

  _Request_:  follows strictly the request payload schema, the OpcNodes attribute being mandatory.

  _Response_: when successful - Status 200 and an empty json (`{}`) as Payload

  _Exceptions_: a response corresponding to an exception will be returned if:

  - request payload contains an endpoint (DataSet) that is not present in publisher configuration

  - request payload contains a node that is not present in publisher configuration

  _Example_:

  > _Method Name_: `UnpublishNodes_V1`
  >
  > _Request_:
  >
  > ```json
  > {
  >   "EndpointUrl":"opc.tcp://opcplc:50000/",
  >   "DataSetWriterGroup":"Asset0",
  >   "DataSetWriterId":"DataFlow0",
  >   "DataSetPublishingInterval":5000,
  >   "OpcNodes":[
  >     {
  >       "Id":"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt0"
  >     }
  >   ]
  > }
  > ```
  >
  > _Response_:
  >
  > ```json
  > {
  >   "status":200,
  >   "payload":{
  >   }
  > }
  > ```

## UnpublishAllNodes_V1

UnpublishAllNodes method enables a client to remove all the nodes from a previously configured DataSetWriter. The DataSetWriter entity will be completely removed from the configuration storage.

  _Request_: follows strictly the request payload schema, the OpcNodes attribute should be excluded.

  _Response_: when successful - Status 200 and an empty json (`{}`) as Payload

  _Exceptions_: a response corresponding to an exception will be returned if:

  - request payload contains an endpoint (DataSet) that is not present in publisher configuration

  - request payload contains OpcNodes

  _Example_:

  > _Method Name_: `UnpublishAllNodes_V1`
  >
  > _Payload_:
  >
  > ```json
  > {
  >   "EndpointUrl":"opc.tcp://opcplc:50000",
  >   "DataSetWriterGroup":"Server0",
  >   "DataSetWriterId":"Device0",
  >   "DataSetPublishingInterval":5000
  > }
  > ```
  >
  > _Response_:
  >
  > ```json
  > {
  >   "status":200,
  >   "payload":{
  >   }
  > }
  > ```

## GetConfiguredEndpoints_V1

Returns the configured endpoints (Datasets)

  _Request_: {}

  _Response_: list of Endpoints configured (and optional parameters).

  _Exceptions_: an exception is thrown when method call returns status other than 200

  _Example_:

  > _Method Name_: `GetConfiguredEndpoints_V1`
  >
  > _Response_:
  >
  > ```json
  > {
  >   "status":200,
  >   "payload":[
  >     {
  >       "EndpointUrl":"opc.tcp://opcplc:50000/",
  >       "DataSetWriterGroup":"Server0",
  >       "DataSetWriterId":"Device0",
  >       "DataSetPublishingInterval":5000
  >     },
  >     {
  >       "EndpointUrl":"opc.tcp://opcplc:50001/"
  >     }
  >   ]
  > }
  > ```

## GetConfiguredNodesOnEndpoint_V1

Returns the nodes configured for one Endpoint (Dataset)

  _Request_: contains the elements necessary to uniquely identify a Dataset. The EndpointUrl is mandatory in the request, the other attributes are optional and can be used to refine your result.

  _Response_: list of OpcNodes configured for the selected Endpoint (and optional parameters).

  _Exceptions_: an exception is thrown when method call returns status other than 200

  _Example_:

  > _Method Name_: `GetConfiguredNodesOnEndpoints_V1`
  >
  > _Payload_:
  >
  > ```json
  > {
  >   "EndpointUrl":"opc.tcp://192.168.100.20:50000"
  > }
  > ```
  >
  > _Response_:
  >
  > ```json
  > {
  >   "status":200,
  >   "payload":[
  >     {
  >       "id":"nsu=http://microsoft.com/Opc/OpcPlc/;s=SlowUInt1",
  >       "opcSamplingInterval":3000,
  >       "opcSamplingIntervalTimespan":"00:00:03",
  >       "heartbeatInterval":0,
  >       "heartbeatIntervalTimespan":"00:00:00"
  >     }
  >   ]
  > }
  > ```

## GetDiagnosticInfo_V1

Returns a list of actual metrics for every endpoint (Dataset) .

  _Request_: none

  _Response_: list of actual metrics for every endpoint (Dataset).

  _Exceptions_: an exception is thrown when method call returns status other than 200

  _Example_:

  > _Method Name_: `GetDiagnosticInfo_V1`
  >
  > _Response_:
  >
  > ```json
  > {
  >   "status":200,
  >   "payload":[
  >     {
  >       "EndpointInfo":{
  >         "EndpointUrl":"opc.tcp://opcplc:50000/",
  >         "DataSetWriterGroup":"Server0",
  >         "UseSecurity":"false",
  >         "OpcAuthenticationMode":"UsernamePassword",
  >         "OpcAuthenticationUsername":"Usr"
  >       },
  >       "SentMessagesPerSec":"2.6",
  >       "IngestionDuration":"{00:00:25.5491702}",
  >       "IngressDataChanges":"25",
  >       "IngressValueChanges":"103",
  >       "IngressBatchBlockBufferSize":"0",
  >       "EncodingBlockInputSize":"0",
  >       "EncodingBlockOutputSize":"0",
  >       "EncoderNotificationsProcessed":"83",
  >       "EncoderNotificationsDropped":"0",
  >       "EncoderIoTMessagesProcessed":"2",
  >       "EncoderAvgNotificationsMessage":"41.5",
  >       "EncoderAvgIoTMessageBodySize":"6128",
  >       "EncoderAvgIoTChunkUsage":"1.158034",
  >       "EstimatedIoTChunksPerDay":"13526.858105160689",
  >       "OutgressBatchBlockBufferSize":"0",
  >       "OutgressInputBufferCount":"0",
  >       "OutgressInputBufferDropped":"0",
  >       "OutgressIoTMessageCount":"0",
  >       "ConnectionRetries":"0",
  >       "OpcEndpointConnected":"true",
  >       "MonitoredOpcNodesSucceededCount":"5",
  >       "MonitoredOpcNodesFailedCount":"0"
  >     }
  >   ]
  > }
  > ```

## AddOrUpdateEndpoints_V1

This method enables to perform a complete `published_nodes.json` update as well as update multiple
endpoints (DataSets) at once. Unlike `PublishNodes_V1` method, `AddOrUpdateEndpoints_V1`  completely
changes the node set for an endpoint (DataSet) with the one provided in the method's request payload.
Furthermore, by providing an empty list of nodes in the request, the user can remove completely the
previously configured nodes for a specific endpoint (DataSet).

  _Request_: represents a list of objects which should strictly follow the request payload schema as
  described above. The `OpcNodes` attribute being empty list or `null` will be interpreted as a removal
  request for that endpoint (DataSet).

  _Response_: when successful - Status 200 and an empty json (`{}`) as payload

  _Exceptions_: a response corresponding to an exception will be returned if:

  - request payload contains deletion request for an endpoint (DataSet) that is not present in publisher configuration

  - request payload contains two or more entries for the same endpoint (DataSet)

  _Example_:
  > _Method Name_: `AddOrUpdateEndpoints_V1`
  >
  > _Payload_:
  >
  > ```json
  > [
  >   {
  >     "EndpointInfo":{
  >       "EndpointUrl":"opc.tcp://opcplc:50000/",
  >       "DataSetWriterGroup":"Server0",
  >       "UseSecurity":"false",
  >       "OpcAuthenticationMode":"UsernamePassword",
  >       "OpcAuthenticationUsername":"Usr"
  >     },
  >     "SentMessagesPerSec":"2.6",
  >     "IngestionDuration":"{00:00:25.5491702}",
  >     "IngressDataChanges":"25",
  >     "IngressValueChanges":"103",
  >     "IngressBatchBlockBufferSize":"0",
  >     "EncodingBlockInputSize":"0",
  >     "EncodingBlockOutputSize":"0",
  >     "EncoderNotificationsProcessed":"83",
  >     "EncoderNotificationsDropped":"0",
  >     "EncoderIoTMessagesProcessed":"2",
  >     "EncoderAvgNotificationsMessage":"41.5",
  >     "EncoderAvgIoTMessageBodySize":"6128",
  >     "EncoderAvgIoTChunkUsage":"1.158034",
  >     "EstimatedIoTChunksPerDay":"13526.858105160689",
  >     "OutgressBatchBlockBufferSize":"0",
  >     "OutgressInputBufferCount":"0",
  >     "OutgressInputBufferDropped":"0",
  >     "OutgressIoTMessageCount":"0",
  >     "ConnectionRetries":"0",
  >     "OpcEndpointConnected":"true",
  >     "MonitoredOpcNodesSucceededCount":"5",
  >     "MonitoredOpcNodesFailedCount":"0"
  >   }
  > ]
  > ```
  >
  > _Response_:
  >
  > ```json
  > {
  >   "status": 200,
  >   "payload": {}
  > }
  > ```
