# Configuration API <!-- omit in toc -->

[Home](./readme.md)

For large-scale deployments, automating the configuration and management of OPC Publisher is critical. OPC Publisher version 2.8.2 and later implements [IoT Hub Direct Methods](https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-direct-methods), which can be called from an application using the [IoT Hub Device SDK](https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-sdks).

Azure IoT Hub's Cloud-to-Device (C2D) commands allow you to remotely configure and control OPC Publisher instances running on IoT Edge devices. For example, you can send commands to update the configuration, restart the module, or change runtime parameters without needing to manually intervene on each device. An example of sending a C2D command to update the configuration:

```bash
az iot hub invoke-module-method --hub-name <your-iot-hub> --device-id <your-device-id> --module-name <opc-publisher> --method-name SetConfiguredEndpoints --method-payload '{"Endpoints": [{"EndpointUrl": "opc.tcp://new-opc-server:4840", "OpcNodes": [{"Id": "ns=2;i=10853"}]}]}'
```

The following direct methods and many more can be used to remotely configure the OPC Publisher:

- [PublishNodes\_V1](#publishnodes_v1)
- [AddOrUpdateEndpoints\_V1](#addorupdateendpoints_v1)
- [UnpublishNodes\_V1](#unpublishnodes_v1)
- [UnpublishAllNodes\_V1](#unpublishallnodes_v1)
- [GetConfiguredEndpoints\_V1](#getconfiguredendpoints_v1)
- [SetConfiguredEndpoints\_V1](#setconfiguredendpoints_v1)
- [GetConfiguredNodesOnEndpoint\_V1](#getconfigurednodesonendpoint_v1)
- [GetDiagnosticInfo\_V1](#getdiagnosticinfo_v1)

The corresponding REST API of OPC Publisher 2.9 (with same operation names and [type definitions](./definitions.md)) is documented [here](./api.md). In addition to calling the API through HTTP REST calls (Preview) you can call the configuration API through MQTT v5 (Preview) in OPC Publisher 2.9.

If you need to migrate your application from OPC Publisher 2.5.x to OPC Publisher 2.8.2 or later, we provide the needed information in a [separate document](./migrationpath.md).

It is important to understand the [configuration schema](./readme.md#configuration-schema) as it is the foundation of the OPC Publisher configuration surface and represented by the [PublishedNodesEntryModel](./definitions.md#publishednodesentrymodel).

Now let's dive into each direct method request and response payloads with examples.

## PublishNodes_V1

PublishNodes enables a client to add a set of nodes to be published. A [`DataSetWriter`](./readme.md#configuration-schema) groups nodes which results in seperate subscriptions being created (grouped further by the Publishing interval, if different ones are configured, but these have no bearing on the `DataSetWriter` identity). A `DataSetWriter`s identity is the combination of `DataSetWriterId`, `DataSetName`, `DataSetKeyFrameCount`, `DataSetClassId`, and connection relevant information such as credentials, security mode, and endpoint Url. To update a `DataSetWriter` this information must match exactly.

When a `DataSetWriter` already exists, the nodes are incrementally added to the same [`dataset`](./readme.md#configuration-schema). When it doesn't already exist, a new `DataSetWriter` is created with the initial set of nodes contained in the request. When working with `DataSetWriterGroup`s it is important to note that all groups not part of the publish request are removed.  To incrementally update `DataSetWriterGroup`s of `DataSetWriter`s use [`AddOrUpdateEndpoints_v1`](#addorupdateendpoints_v1) API instead.

  _Request_: follows strictly the request [payload schema](./definitions.md#publishednodesentrymodel), the `OpcNodes` attribute being mandatory.

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

More information can be found in the [API documentation](./api.md#handler-for-publishnodes-direct-method)

## AddOrUpdateEndpoints_V1

This method allows updating multiple endpoints (`DataSetWriter`s) without effecting others. Unlike `PublishNodes_V1` method, `AddOrUpdateEndpoints_V1` replaces the nodes of an endpoint (`DataSetWriter`) with the one provided in the method's request payload. By providing an empty list of nodes in a request, a endpoint (`DataSetWriter`) can be removed from a `DataSetWriterGroup`. Removing the last from a group removes the group.

  _Request_: represents a list of objects, which should strictly follow the request payload schema as described above. The `OpcNodes` attribute being empty list or `null` will be interpreted as a removal request for that endpoint (`DataSetWriter`).

  _Response_: when successful - Status 200 and an empty json (`{}`) as payload

  _Exceptions_: a response corresponding to an exception will be returned if:

- request payload contains deletion request for an endpoint (`DataSetWriter`) that isn't present in publisher configuration

- request payload contains two or more entries for the same endpoint (`DataSetWriter`)

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

More information can be found in the [API documentation](./api.md#handler-for-addorupdateendpoints-direct-method)

## UnpublishNodes_V1

UnpublishNodes method enables a client to remove nodes from a previously configured `DataSetWriter`. A `DataSetWriter`s identity is the combination of `DataSetWriterId`, `DataSetName`, `DataSetKeyFrameCount`, `DataSetClassId` and connection relevant information such as credentials, security mode, and endpoint Url. To update a `DataSetWriter` this information must match exactly.
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

More information can be found in the [API documentation](./api.md#handler-for-unpublishallnodes-direct-method)

## UnpublishAllNodes_V1

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

More information can be found in the [API documentation](./api.md#handler-for-unpublishallnodes-direct-method)

## GetConfiguredEndpoints_V1

Returns the configured endpoints (`DataSetWriter`s)

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
  >             "dataSetPublishingInterval": 5000,
  >           },
  >           {
  >              "endpointUrl": "opc.tcp://opcplc:50001"
  >           }
  >       ]
  >    }
  > }
  > ```

More information can be found in the [API documentation](./api.md#handler-for-getconfiguredendpoints-direct-method)

## SetConfiguredEndpoints_V1

Sets the configured endpoints (`DataSetWriter`s) and thus allows to update all configuration at once. The configuration on the OPC Publisher is replaced with the request payload content. Use empty array of endpoints to clear the entire content of the published nodes file.

  _Request_: A list of configured endpoints (and optional parameters).

  _Response_: empty json (`{}`)

  _Exceptions_: an exception is thrown when method call returns status other than 200

  _Example_:

  > _Method Name_: `SetConfiguredEndpoints_V1`
  >
  > _Payload_:
  >
  > ```json
  > {
  >    "endpoints": [
  >       {
  >          "endpointUrl": "opc.tcp://opcplc:50000",
  >          "dataSetWriterGroup": "Asset1",
  >          "dataSetWriterId": "DataFlow1",
  >          "dataSetPublishingInterval": 5000,
  >        },
  >        {
  >           "endpointUrl": "opc.tcp://opcplc:50001"
  >        }
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

More information can be found in the [API documentation](./api.md#handler-for-setconfiguredendpoints-direct-method)

## GetConfiguredNodesOnEndpoint_V1

Returns the nodes configured for one Endpoint (`DataSetWriter`s).

  _Request_: contains the elements defining the Dataset. Please note that the Dataset definition should fully match the one that is present in OPC Publisher configuration.

  _Response_: list of `OpcNodes` configured for the selected Endpoint (and optional parameters).

  _Exceptions_: an exception is thrown when method call returns status other than 200

  _Example_:

  > _Method Name_: `GetConfiguredNodesOnEndpoints_V1`
  >
  > _Payload_:
  >
  > ```json
  > {
  >    "EndpointUrl": "opc.tcp://192.168.100.20:50000",
  >    "DataSetWriterGroup": "Asset0",
  >    "DataSetWriterId": "DataFlow0",
  >    "DataSetPublishingInterval": 5000
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

More information can be found in the [API documentation](./api.md#handler-for-getconfigurednodesonendpoint-direct-method)

## GetDiagnosticInfo_V1

Returns a list of actual metrics for all concrete `DataSetWriter`s. This includes virtual `DataSetWriter`s created due to different publishing intervals configured.  The name of these contain the original name provided and a hash value.

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
  >          "monitoredOpcNodesFailedCount": 0,
  >          "ingressEventNotifications": 0,
  >          "ingressEvents": 0
  >       }
  >    ]
  > }
  > ```

More information can be found in the [API documentation](./api.md#handler-for-getdiagnosticinfo-direct-method)
