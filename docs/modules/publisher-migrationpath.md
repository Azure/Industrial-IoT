[Home](../../readme.md)

# Application migration from OPC Publisher 2.5.x direct methods to 2.8.2 direct methods

Migration of OPC Publisher version 2.5.x to 2.8.2 should work in standalone mode for backwards compatibility with 2.5.x functionality.

## Configuration file (pn.json)

**TODO**

## Command Line Arguments

**TODO**

## OPC UA Certificates management

**TODO**

## Direct Method compatibility

OPC Publisher version 2.8.2 and above implements [IoT Hub Direct Methods](https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-direct-methods), which can be called from applications using the [IoT Hub Device SDK](https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-sdks).

The direct method request payload of OPC Publisher 2.8.2 and above is backwards compatible with OPC Publisher 2.5.x direct methods. The payload schema allows also configuration of attributes introduced in `pn.json` in OPC Publisher 2.6.x and above (for example: DataSetWriterGroup, DataSetWriterId, QueueSize per node, ...)

**Limitations:** Continuation points for GetConfiguredEndpoints and GetConfiguredNodesOnEndpoint aren't available in 2.8.2

## OPC Publisher 2.5.x direct methods supported in 2.8.2

The following table describes the direct methods, which were available in OPC Publisher 2.5.x with their request and response payloads.

| **MethodName**                   | **Request**                                                     | **Response**                                                                                                                                                                 | **in 2.8.2 and above** |
|----------------------------------|-----------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------|
| **PublishNodes**                 | EndpointUrl, List\<OpcNodes\>,  UseSecurity, UserName, Password | Status, List\<StatusResponse\>                                                                                                                                               | Yes                    |
| **UnpublishNodes**               | EndpointUrl, List\<OpcNodes\>                                   | Status, List\<StatusResponse\>                                                                                                                                               | Yes                    |
| **UnpublishAllNodes**            | EndpointUrl                                                     | Status, List\<StatusResponse\>                                                                                                                                               | Yes                    |
| **GetConfiguredEndpoints**       | -                                                               | List\<EndpointUrl\>                                                                                                                                                          | Yes                    |
| **GetConfiguredNodesOnEndpoint** | EndpointUrl                                                     | EndpointUrl, List< OpcNodeOnEndpointModel > where OpcNodeOnEndpointModel contains: Id ExpandedNodeId OpcSamplingInterval OpcPublishingInterval DisplayName HeartbeatInterval | Yes                    |
| **GetDiagnosticInfo**            | -                                                               | DiagnosticInfoMethodResponseModel                                                                                                                                            | Yes                    |
| **GetDiagnosticLog**             | -                                                               | MissedMessageCount, LogMessageCount, List\<Log\>                                                                                                                             | No*                    |
| **GetDiagnosticStartupLog**      | -                                                               | MissedMessageCount, LogMessageCount, List\<Log\>                                                                                                                             | No*                    |
| **ExitApplication**              | SecondsTillExit (optional)                                      | StatusCode, List\<StatusResponse\>                                                                                                                                           | No*                    |
| **GetInfo**                      | -                                                               | GetInfoMethodResponseModel                                                                                                                                                   | No*                    |

*This functionality is provided by direct methods of the IoT Edge `edgeAgent` module. For more information, see ["Communicate with edgeAgent using built-in direct methods"](https://docs.microsoft.com/azure/iot-edge/how-to-edgeagent-direct-method).

An outdated, archived [sample application](https://github.com/Azure-Samples/iot-edge-opc-publisher-nodeconfiguration) used to configure OPC Publisher 2.5.x can be used to configure OPC Publisher 2.8.2.

### Direct Methods use in applications

For new applications, direct method names with a `_V1` suffix should be used. For backward compatibility of older applications direct method names without the `_V1` suffix are supported, but are subject of deprecation.

### PublishNodes (PublishNodes_V1)

`Request`

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

`Response` (2.5.x)

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

`Response` (2.8.2)

```json
{
   "status": 200,
   "payload": {}
}
```

### GetConfiguredEndpoints (GetConfiguredEndpoints_V1)

`Request`

 {}

`Response` (2.5.x)

Without any configured endpoints:

```json
{
   "status": 200,
   "payload":
   {
      "Endpoints": []
   }
}
```

With configured endpoints:

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

`Response` (2.8.2)

Without configured endpoints:

```json
{
   "status": 200,
   "payload":
   {
      "endpoints": []
   }
}
```

With configured endpoints:

```json
{
   "status": 200,
   "payload":
   {
      "endpoints": [
         {
            "endpointUrl":"opc.tcp://sandboxhost-637811493394507132:50000"
         }
      ]
   }
}
```

### GetConfiguredNodesOnEndpoint (GetConfiguredNodesOnEndpoint_V1)

`Request`

```json
{
   "EndpointUrl": "opc.tcp://sandboxhost-637811493394507132:50000"
}
```

`Response` (2.5.x)

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

`Response` (2.8.2)

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

If the endpoint isn't configured or `UnpublishAllNodes` is called on that endpoint, then the response will be:

```json
{
   "status": 404,
   "payload": "Endpoint not found: opc.tcp://sandboxhost-637811493394507132:50000/"
}
```

### GetDiagnosticInfo (GetDiagnosticInfo_V1)

`Request`
{}

`Response`  (2.5.x)

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

`Response` (2.8.2)

```json
{
   "status": 200,
   "payload": [
      {
         "endpoint": {
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

### UnpublishNodes (UnpublishNodes_V1)

If _OpcNodes_ is omitted, then all nodes are unpublished and the endpoint is removed.

`Request`

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

`Response` (2.5.x)

```json
{
   "status": 200,
   "payload": [
      "Id 'nsu=http://microsoft.com/Opc/OpcPlc/;s=65e451f1-56f1-ce84-a44f-6addf176beaf': tagged for removal"
   ]
}
```

`Response` (2.8.2)

```json
{
   "status": 200,
   "payload": {}
}
```

### UnpublishAllNodes (UnpublishAllNodes_V1)

`Request`

```json
{
   "EndpointUrl": "opc.tcp://sandboxhost-637811493394507132:50000"
}
```

`Response` (2.5.x)

```json
{
   "status": 200,
   "payload": [
      "All monitored items in all subscriptions on endpoint 'opc.tcp://sandboxhost-637811493394507132:50000' tagged for removal"
   ]
}
```

`Response` (2.8.2)

```json
{
   "status": 200,
   "payload": {}
}
```

## OPC Publisher 2.5.x direct methods not supported in 2.8.2

The direct methods not available in OPC Publisher 2.8.2, there are changes required to apply to the applications, which do use them.

### GetDiagnosticLog

**TODO**

### GetDiagnosticStartupLog

**TODO**

### ExitApplication

**TODO**

### GetInfo

**TODO**
