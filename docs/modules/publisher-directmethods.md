[Home](../../readme.md)

# Configuration via IoT Hub Direct methods

OPC Publisher v2.8.2 implements the following [IoT Hub Direct Methods](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-direct-methods) which can be called from an application (anywhere in the world) leveraging the [IoT Hub Device SDK](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-sdks).

There are some direct methods which are inherited from 2.5.x in addition to new ones.

Inherited direct methods from 2.5.x:

- PublishNodes_V1
- UnpublishNodes_V1
- UnpublishAllNodes_V1
- GetConfiguredEndpoints_V1
- GetConfiguredNodesOnEndpoints_V1
- GetDiagnosticInfo_V1

New direct methods:

-  AddOrUpdateEndpoints_V1

Note: `_V1` suffix is not required.

To understand the terminologies and migration path from version 2.5.x of OPC Publisher to 2.8.2, please refer to [this](publisher-dmmigration.md) migration document.


Now let's dive into each direct method request and response payloads with examples.

## PublishNodes_V1

PublishNodes enables a client to add a set of nodes to be published for a specific [`DataSetWriter`](publisher-dmmigration.md#terminologies). When a `DataSetWriter` already exists, the nodes are incrementally added to the very same [`dataset`](publisher-dmmigration.md#terminologies). When it does not already exist, a new `DataSetWriter` is created with the initial set of nodes contained in the request.

  *Request:* follows strictly the request [payload schema](publisher-dmmigration.md#payload-schema), the OpcNodes attribute being mandatory.

  *Response:* when successful Status 200 and an empty json (`{}`) as payload

  *Exceptions:* an exception is thrown when method call returns status other than 200

  *Example:*

  > *Method Name:* `PublishNodes_V1`
  >
  > *Request:*
  >
  > ```json
  > {
  >   "EndpointUrl": "opc.tcp://opcplc:50000/",
  >   "DataSetWriterGroup": "Asset0",
  >   "DataSetWriterId": "DataFlow0",
  >   "DataSetPublishingInterval": 5000,
  >   "OpcNodes":
  >   [
  >     {
  >         "Id": "nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt0",
  >     }
  >   ]
  > }
  > ```
  >
  > *Response:*
  >
  > ```json
  > {
  >   "status": 200,
  >   "payload": {}
  > }
  > ```

## UnpublishNodes_V1

UnpublishNodes method enables a client to remove nodes from a previously configured DataSetWriter.

*Note*: If all the nodes from a DataSet are to be unpublished, the DataSetWriter entity is completely removed from the configuration storage.

  *Request:*  follows strictly the request payload schema, the OpcNodes attribute being mandatory.

  *Response:* when successful - Status 200 and an empty json (`{}`) as Payload

  *Exceptions:* a response corresponding to an exception will be returned if:
  - request payload contains an endpoint (DataSet) that is not present in publisher configuration
  - request payload contains a node that is not present in publisher configuration

  *Example:*

  > *Method Name:* `UnpublishNodes_V1`
  > *Request:*
  >
  > ```json
  > {
  >   "EndpointUrl": "opc.tcp://opcplc:50000/",
  >   "DataSetWriterGroup": "Asset0",
  >   "DataSetWriterId": "DataFlow0",
  >   "DataSetPublishingInterval": 5000,
  >   "OpcNodes":
  >   [
  >     {
  >         "Id": "nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt0",
  >     }
  >   ]
  > }
  > ```
  >
  > *Response:*
  >
  > ```json
  > {
  >   "status": 200,
  >   "payload": {}
  > }
  > ```

## UnpublishAllNodes_V1

UnpublishAllNodes method enables a client to remove all the nodes from a previously configured DataSetWriter. The DataSetWriter entity will be completely removed from the configuration storage.

  *Request:* follows strictly the request payload schema, the OpcNodes attribute should be excluded.

  *Response:* when successful - Status 200 and an empty json (`{}`) as Payload

  *Exceptions:* a response corresponding to an exception will be returned if:
  - request payload contains an endpoint (DataSet) that is not present in publisher configuration
  - request payload contains OpcNodes

  *Example:*

  > *Method Name:* `UnpublishAllNodes_V1`
  >
  > *Payload*:
  >
  > ```json
  > {
  >   "EndpointUrl": "opc.tcp://opcplc:50000",
  >   "DataSetWriterGroup": "Server0",
  >   "DataSetWriterId": "Device0",
  >   "DataSetPublishingInterval": 5000
  > }
  > ```
  >
  > *Response:*
  >
  > ```json
  > [
  >   "Unpublishing all nodes succeeded for EndpointUrl: opc.tcp://opcplc:50000/"
  > ]
  > ```

## GetConfiguredEndpoints_V1

Returns the configured endpoints (Datasets)

  *Request:* {}

  *Response:* list of Endpoints configured (and optional parameters).

  *Exceptions:* an exception is thrown when method call returns status other than 200

  *Example:* 

  > *Method Name:* `GetConfiguredEndpoints_V1`
  > *Response:*
  >
  > ```json
  > {
  >   "status":200,
  >   "payload":
  >   [
  >      {
  >          "EndpointUrl": "opc.tcp://opcplc:50000/",
  >          "DataSetWriterGroup": "Server0",
  >          "DataSetWriterId": "Device0",
  >          "DataSetPublishingInterval": 5000
  >      },
  >      {
  >          "EndpointUrl": "opc.tcp://opcplc:50001/"
  >      }
  >   ]
  > }
  > ```

## GetConfiguredNodesOnEndpoints_V1

Returns the nodes configured for one Endpoint (Dataset)

  *Request:* contains the elements necessary to uniquely identify a Dataset. The EndpointUrl is mandatory in the request, the other attributes are optional and can be used to refine your result.

  *Response:* list of OpcNodes configured for the selected Endpoint (and optional parameters).

  *Exceptions:* an exception is thrown when method call returns status other than 200

  *Example:*

  > *Method Name:* `GetConfiguredNodesOnEndpoints_V1`
  > *Payload:*
  >
  > ```json
  > {
  >   "EndpointUrl": "opc.tcp://192.168.100.20:50000"
  > }
  > ```
  >
  > *Response:*
  >
  > ```json
  > {
  >   "status":200,
  >   "payload":
  >   [
  >     {
  >        "id":"nsu=http://microsoft.com/Opc/OpcPlc/;s=SlowUInt1",
  >        "opcSamplingInterval":3000,
  >        "opcSamplingIntervalTimespan":"00:00:03",
  >        "heartbeatInterval":0,
  >        "heartbeatIntervalTimespan":"00:00:00"
  >     }
  >   ]
  > }
  > ```

## GetDiagnosticInfo_V1

Returns a list of actual metrics for every endpoint (Dataset) . 

  *Request:* none

  *Response:* list of actual metrics for every endpoint (Dataset).

  *Exceptions:* an exception is thrown when method call returns status other than 200

  *Example:*

  > *Method Name:* `GetDiagnosticInfo_V1`
  > *Response:*
  >
  > ```json
  > {
  > "status":200,
  > "payload":
  > [
  >    {
  >      "EndpointInfo":
  >       {
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

  *Request:* represents a list of objects which should strictly follow the request payload schema as
  described above. The `OpcNodes` attribute being empty list or `null` will be interpreted as a removal
  request for that endpoint (DataSet).

  *Response:* when successful -  Status 200 and an empty json (`{}`) as payload

  *Exceptions:* a response corresponding to an exception will be returned if:
  - request payload contains deletion request for an endpoint (DataSet) that is not present in publisher configuration
  - request payload contains two or more entries for the same endpoint (DataSet)

  *Example:*
  > *Method Name:* `AddOrUpdateEndpoints_V1`
  > *Payload:*
  >
  >```json
  >  [
  >    {
  >      "EndpointUrl": "opc.tcp://opcplc:50000/",
  >      "DataSetWriterGroup": "Server0",
  >      "DataSetWriterId": "Device0",
  >      "DataSetPublishingInterval": 5000,
  >      "OpcNodes":
  >      [
  >        {
  >          "Id": "nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt0",
  >        }
  >      ]
  >    },
  >    {
  >      "EndpointUrl": "opc.tcp://opcplc:50001/",
  >      "DataSetWriterGroup": "Server1",
  >      "DataSetWriterId": "Device1",
  >      "OpcNodes": []
  >    }
  >  ]
  >```
  > *Response:*
  >
  > ```json
  > {
  >   "status": 200,
  >   "payload": {}
  > }
  > ```


