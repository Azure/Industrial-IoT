[Home](../../readme.md)

# Migration path for publisher version 2.5.x to 2.8.2 - configuration file structure (pn.json)
**TODO**
2.8.2 must work in standalone mode for backwards compatibility with 2.5.x functionality

# Migration path for publisher version 2.5.x to 2.8.2 - CLI arguments
**TODO**

# Migration path for publisher version 2.5.x to 2.8.2 - OPC UA Certificates management
**TODO**

# Migration path for publisher version 2.5.x to 2.8.2 - Direct Methods API

The latest version 2.8.2 adds support for configuration via IoT Hub direct methods. OPC Publisher implements multiple [IoT Hub Direct Methods](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-direct-methods) which can be called from an application leveraging the [IoT Hub Device SDK](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-sdks). This document provides the migration path from 2.5.x to 2.8.2.

The direct methods' request payload of version 2.8.2 is backwards compatible with the 2.5.x direct methods. The payload schema allows configuration of additional extensions introduced in the `pn.json` in the publisher 2.6.x and newer e.g. DataSetWriterGroup, DataSetWriterId, QueueSize per node, etc.

For this set of methods, the encoding is JSON and no compression or payload chunking mechanism is applied in order to ensure the backwards compatibility with the 2.5.x version of OPC Publisher module.

## Direct Methods of version 2.5.x

The following  table describes the direct methods which were available in OPC Publisher 2.5.x with request and response.

| 2.5.x                            |                                                                 |                                                              |              |
| -------------------------------- | --------------------------------------------------------------- | ------------------------------------------------------------ | ------------ |
| **MethodName**                   | **Request**                                                     | **Response**                                                 | **in 2.8.2** |
| **PublishNodes**                 | EndpointUrl, List\<OpcNodes\>,  UseSecurity, UserName, Password | Status, List\<StatusResponse\>                               | Yes          |
| **UnpublishNodes**               | EndpointUrl, List\<OpcNodes\>                                   | Status, List\<StatusResponse\>                               | Yes          |
| **UnpublishAllNodes**            | EndpointUrl                                                     | Status, List\<StatusResponse\>                               | Yes          |
| **GetConfiguredEndpoints**       | -                                                               | List\<EndpointUrl\>                                          | Yes          |
| **GetConfiguredNodesOnEndpoint** | EndpointUrl                                                     | EndpointUrl, List< OpcNodeOnEndpointModel >    where OpcNodeOnEndpointModel contains:    Id ExpandedNodeId OpcSamplingInterval OpcPublishingInterval DisplayName HeartbeatInterval SkipFirst | Yes          |
| **GetDiagnosticInfo**            | -                                                               | DiagnosticInfoMethodResponseModel                            | Yes          |
| **GetDiagnosticLog**             | -                                                               | MissedMessageCount, LogMessageCount, List\<Log\>             | No*          |
| **GetDiagnosticStartupLog**      | -                                                               | MissedMessageCount, LogMessageCount, List\<Log\>             | No*          |
| **ExitApplication**              | SecondsTillExit (optional)                                      | StatusCode, List\<StatusResponse\>                           | No*          |
| **GetInfo**                      | -                                                               | GetInfoMethodResponseModel >>    VersionMajor  VersionMinor  VersionPatch  SemanticVersion  InformationalVersion OS  OSArchitecture  FrameworkDescription | No*          |

*This functionality is provided by the IoT Edge `edgeAget` module via its own direct methods, see [this page](https://docs.microsoft.com/azure/iot-edge/how-to-edgeagent-direct-method) for more information.

A [sample configuration application](https://github.com/Azure-Samples/iot-edge-opc-publisher-nodeconfiguration) as well as an [sample application for reading diagnostic information](https://github.com/Azure-Samples/iot-edge-opc-publisher-diagnostics) are provided from OPC Publisher leveraging this interface.

_Please note that the samples applications are not actively maintained and may be outdated._

## Direct Methods of version 2.8.2

The following set of direct method api is inherited from 2.5.x API set.
- PublishNodes_V1
- UnpublishNodes_V1
- UnpublishAllNodes_V1
- GetConfiguredEndpoints_V1
- GetConfiguredNodesOnEndpoint_V1
- GetDiagnosticInfo_V1

Please note that in 2.8.2 the recommended way of using the direct methods is with `_V1` suffix however.
Direct method name without `_V1` suffix is also supported but is subject of deprication.

The direct methods definitions of version 2.8.2 are provided in [this](publisher-directmethods.md) document in detail.

# Direct methods of version 2.5.x vs 2.8.2

In this section let's see the request and response payloads for the direct methods of version 2.5.x and 2.8.2

## PublishNodes_V1

`Request`:

```json
{
   "EndpointUrl": "opc.tcp://sandboxhost-637811493394507132:50000",
   "UseSecurity": false,
   "OpcNodes":[
      {
         "Id": "nsu=http://microsoft.com/Opc/OpcPlc/Boiler;s=Boiler"
      },
      {
         "Id": "nsu=http://microsoft.com/Opc/OpcPlc/;s=65e451f1-56f1-ce84-a44f-6addf176beaf"
      },
      {
         "Id": "nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt1"
      }
   ]
}
```

`Response` in **2.5.x**:

```json
{
   "status": 200,
   "payload": [
      "'nsu=http://microsoft.com/Opc/OpcPlc/Boiler;s=Boiler': added",
      "'nsu=http://microsoft.com/Opc/OpcPlc/;s=65e451f1-56f1-ce84-a44f-6addf176beaf': added",
      "'nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt1': added"
   ]
}
```

`Response` in **2.8.2**:

```json
{
   "status": 200,
   "payload": {}
}
```

## GetConfiguredEndpoints_V1

`Request`: {}

`Response` in **2.5.x**:

When there are no configured endpoints:

```json
{
   "status": 200,
   "payload": 
   {
      "Endpoints": []
   }
}
```

When there are configured endpoints:

```json
{
   "status": 200,
   "payload": 
   {
      "Endpoints": [
         {
            "EndpointUrl":"opc.tcp://sandboxhost-637811493394507132:50000"
         }
      ]
   }
}
```

`Response` in **2.8.2**:

When there are no configured endpoints:

```json
{
   "status": 200,
   "payload": 
   {
      "endpoints": []
   }   
}
```

When there are configured endpoints:

```json
{
   "status": 200,
   "payload": 
   {
      "endpoints": [
         {
            "endpointUrl":"opc.tcp://sandboxhost-637811493394507132:50000/"
         }
      ]
   ]
}
```

## GetConfiguredNodesOnEndpoint_V1

`Request`:

```json
{
   "EndpointUrl": "opc.tcp://sandboxhost-637811493394507132:50000"
}
```

`Response` in **2.5.x**:

```json
{
   "status": 200,
   "payload": {
      "EndpointUrl": "opc.tcp://sandboxhost-637811493394507132:50000",
      "OpcNodes": [
         {
            "Id": "nsu=http://microsoft.com/Opc/OpcPlc/Boiler;s=Boiler"
         },
         {
            "Id": "nsu=http://microsoft.com/Opc/OpcPlc/;s=65e451f1-56f1-ce84-a44f-6addf176beaf"
         },
         {
            "Id": "nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt1"
         }
      ]
   }
}
```

If `UnpublishAllNodes` is called on that endpoint, then the response will be:

```json
{
   "status": 200,
   "payload": {
      "endpointUrl": "opc.tcp://sandboxhost-637811493394507132:50000",
      "opcNodes": []
   }
}
```

`Response` in **2.8.2**:

```json
{
   "status":200,
   "payload": 
   {
      "opcNodes": [
         {
            "id": "nsu=http://microsoft.com/Opc/OpcPlc/Boiler;s=Boiler"
         },
         {
            "id": "nsu=http://microsoft.com/Opc/OpcPlc/;s=65e451f1-56f1-ce84-a44f-6addf176beaf"
         },
         {
            "id": "nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt1"
         }
      ]
   }
}
```

If the endpoint is not configured or `UnpublishAllNodes` is called on that endpoint, then the response will be:

```json
{
   "status": 404,
   "payload": "Endpoint not found: opc.tcp://sandboxhost-637811493394507132:50000/"
}
```

## GetDiagnosticInfo_V1

`Request`: {}

`Response`  in **2.5.x**:

```json
{
   "status": 200,
   "payload": {
      "PublisherStartTime": "2022-02-22T18:07:56.363511Z",
      "NumberOfOpcSessionsConfigured": 1,
      "NumberOfOpcSessionsConnected": 0,
      "NumberOfOpcSubscriptionsConfigured": 1,
      "NumberOfOpcSubscriptionsConnected": 0,
      "NumberOfOpcMonitoredItemsConfigured": 3,
      "NumberOfOpcMonitoredItemsMonitored": 0,
      "NumberOfOpcMonitoredItemsToRemove": 0,
      "MonitoredItemsQueueCapacity": 8192,
      "MonitoredItemsQueueCount": 0,
      "EnqueueCount": 13060,
      "EnqueueFailureCount": 0,
      "NumberOfEvents": 13060,
      "SentMessages": 30,
      "SentLastTime": "2022-02-22T19:03:39.3249082Z",
      "SentBytes": 3000192,
      "FailedMessages": 0,
      "TooLargeCount": 0,
      "MissedSendIntervalCount": 8,
      "WorkingSetMB": 118,
      "DefaultSendIntervalSeconds": 10,
      "HubMessageSize": 262144,
      "HubProtocol": 3
   }
}
```

`Response` in **2.8.2**:

```json
{
   "status": 200,
   "payload": [
      {
         "endpointInfo": {
            "endpointUrl": "opc.tcp://sandboxhost-637811493394507132:50000",
            "dataSetWriterGroup": "Asset1",
            "useSecurity": false,
            "opcAuthenticationMode": "UsernamePassword",
            "opcAuthenticationUsername": "Usr"
         },
         "sentMessagesPerSec": 2.6,
         "ingestionDuration": "{00:00:25.5491702}",
         "ingressDataChanges": 25,
         "ingressValueChanges": 103,
         "ingressBatchBlockBufferSize": 0,
         "encodingBlockInputSize": 0,
         "encodingBlockOutputSize": 0,
         "encoderNotificationsProcessed": 83,
         "encoderNotificationsDropped": 0,
         "encoderIoTMessagesProcessed": 2,
         "encoderAvgNotificationsMessage": 41.5,
         "encoderAvgIoTMessageBodySize": 6128,
         "encoderAvgIoTChunkUsage": 1.158034,
         "estimatedIoTChunksPerDay": 13526.858105160689,
         "outgressBatchBlockBufferSize": 0,
         "outgressInputBufferCount": 0,
         "outgressInputBufferDropped": 0,
         "outgressIoTMessageCount": 0,
         "connectionRetries": 0,
         "opcEndpointConnected": true,
         "monitoredOpcNodesSucceededCount": 5,
         "monitoredOpcNodesFailedCount": 0
      }
   ]
}
```

## UnpublishNodes_V1

If _OpcNodes_ is not provided then all nodes are unpublished and endpoint is removed.

`Request`:

```json
{
   "EndpointUrl": "opc.tcp://sandboxhost-637811493394507132:50000",
   "OpcNodes": [
      {
         "Id":"nsu=http://microsoft.com/Opc/OpcPlc/;s=65e451f1-56f1-ce84-a44f-6addf176beaf"
      }
   ]
}
```

`Response` in **2.5.x**:

```json
{
   "status":200,
   "payload":[
      "Id 'nsu=http://microsoft.com/Opc/OpcPlc/;s=65e451f1-56f1-ce84-a44f-6addf176beaf': tagged for removal"
   ]
}
```

`Response` in **2.8.2**:

```json
{
   "status":200,
   "payload":{
   }
}
```

## UnpublishAllNodes_V1

`Request`:

```json
{
   "EndpointUrl":"opc.tcp://sandboxhost-637811493394507132:50000"
}
```

`Response` in **2.5.x**:

```json
{
   "status":200,
   "payload":[
      "All monitored items in all subscriptions on endpoint 'opc.tcp://sandboxhost-637811493394507132:50000' tagged for removal"
   ]
}
```

`Response` in **2.8.2**:

```json
{
   "status":200,
   "payload":{
   }
}
```

The following direct methods present in 2.5.x were discontinued. 
The alternatives to these actions are detailed below:

## GetDiagnosticLog

**TODO**

## GetDiagnosticStartupLog

**TODO**

## ExitApplication

**TODO**

## GetInfo

**TODO**
