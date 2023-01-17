[Home](../../readme.md)

# Application migration from OPC Publisher 2.5.x direct methods to 2.8.2 direct methods

Migration of OPC Publisher(standalone mode) version 2.5.x to 2.8.2 works for backwards compatibility with 2.5.x functionality.

## Configuration file (pn.json)

In standalone mode OPC Publisher 2.8.2 and above can consume published nodes JSON files of 2.5.x version without any modifications.
OPC Publisher 2.8.2 and above added several new fields to configure publishing but maintained backwards compatibilty with 2.5.x.
The full schema looks like this:

```json
[
  {
    "EndpointUrl": "string",
    "UseSecurity": "boolean",
    "OpcAuthenticationMode": "string",
    "OpcAuthenticationUsername": "string",
    "OpcAuthenticationPassword": "string",
    "DataSetWriterGroup": "string",
    "DataSetWriterId": "string",
    "DataSetPublishingInterval": "integer",
    "DataSetPublishingIntervalTimespan": "string",
    "Tag": "string",
    "OpcNodes": [
      {
        "Id": "string",
        "ExpandedNodeId": "string",
        "DataSetFieldId ": "string",
        "DisplayName": "string",
        "OpcSamplingInterval": "integer",
        "OpcSamplingIntervalTimespan": "string",
        "OpcPublishingInterval": "integer",
        "OpcPublishingIntervalTimespan": "string",
        "HeartbeatInterval": "integer",
        "HeartbeatIntervalTimespan": "string",
        "SkipFirst": "bool",
        "QueueSize": "integer"
      }
    ]
  }
]
```

For details of each field you can consult our [direct methods API documentation](publisher-directmethods.md)
as the fields of published nodes JSON schema map directly to that of direct method API calls.
The only difference is that `OpcAuthenticationUsername` and `OpcAuthenticationPassword` are refereed to as
`UserName` and `Password` in direct method API calls.

Please note that OPC Publisher 2.8.2 can still consume legacy `NodeId`-based node definitions
(as can be found in [`publishednodes_2.5.json`](publishednodes_2.5.json?raw=1)),
but we strongly recommend to use `OpcNodes`-based definitions instead.
Please consider migrating your old published nodes JSON files that use `NodeId` to the newer schema.

## Command Line Arguments

To learn more about how to use comman-line arguments to configure OPC Publisher, please refer to [this](publisher-commandline.md) doc.

## OPC Publisher 2.5.x Command Line Arguments supported in 2.8.2

The following table describes the command line arguments, which were available in OPC Publisher 2.5.x and their compatibility in OPC Publisher 2.8.2 and above.

| **Command Line Options**                |  **in 2.8.2 and above**  | **Alternative** |
|--------------------------------------   |--------------------------|-----------------|
| --pf, --publishfile=VALUE               |  yes                     |                 |
| --tc, --telemetryconfigfile=VALUE       |  no                      |                 |
| --s, --site=VALUE                       |  yes                     |                 |
| --ic, --iotcentral                      |  no                      |                 |
| --sw, --sessionconnectwait=VALUE        |  no                      |                 |
| --mq, --monitoreditemqueuecapacity=VALUE|  no                      | use --om, --maxoutgressmessages=VALUE       |
| --di, --diagnosticsinterval=VALUE       |  yes                     |                 |
| --ns, --noshutdown=VALUE                |  no                      |                 |
| --rf, --runforever                      |  no                      |                 |
| --lf, --logfile=VALUE                   |  yes                     |                 |
| --lt, --logflushtimespan=VALUE          |  yes                     |                 |
| --ll, --loglevel=VALUE                  |  yes                     |                 |
| --ih, --iothubprotocol=VALUE            |  yes                     |                 |
| --ms, --iothubmessagesize=VALUE         |  yes                     |                 |
| --si, --iothubsendinterval=VALUE        |  yes                     |                 |
| --dc, --deviceconnectionstring=VALUE    |  yes                     |                 |
| --c, --connectionstring=VALUE           |  no                      | use --dc, --deviceconnectionstring=VALUE               |
| --hb, --heartbeatinterval=VALUE         |  yes                     |                 |
| --sf, --skipfirstevent=VALUE            |  yes (2.9.0 or above)    | same as --skipfirst=VALUE |
| --pn, --portnum=VALUE                   |  no                      |                 |
| --pa, --path=VALUE                      |  no                      |                 |
| --lr, --ldsreginterval=VALUE            |  no                      |                 |
| --ol, --opcmaxstringlen=VALUE           |  yes                     |                 |
| --ot, --operationtimeout=VALUE          |  yes                     |                 |
| --oi, --opcsamplinginterval=VALUE       |  yes                     |                 |
| --op, --opcpublishinginterval=VALUE     |  yes                     |                 |
| --ct, --createsessiontimeout=VALUE      |  yes                     |                 |
| --ki, --keepaliveinterval=VALUE         |  yes                     |                 |
| --kt, --keepalivethreshold=VALUE        |  yes                     |                 |
| --aa, --autoaccept                      |  yes                     |                 |
| --tm, --trustmyself=VALUE               |  yes                     |                 |
| --to, --trustowncert                    |  no                      | same as --tm, --trustmyself                |
| --fd, --fetchdisplayname=VALUE          |  yes                     |                 |
| --fn, --fetchname                       |  no                      | same as --fd, --fetchdisplayname              |
| --ss, --suppressedopcstatuscodes=VALUE  |  no                      |                 |
| --at, --appcertstoretype=VALUE          |  yes                     |                 |
| --ap, --appcertstorepath=VALUE          |  yes                     |                 |
| --tp, --trustedcertstorepath=VALUE      |  yes                     |                 |
| --rp, --rejectedcertstorepath=VALUE     |  yes                     |                 |
| --ip, --issuercertstorepath=VALUE       |  yes                     |                 |
| --csr                                   |  no                      |                 |
| --ab, --applicationcertbase64=VALUE     |  no                      |                 |
| --af, --applicationcertfile=VALUE       |  no                      |                 |
| --pb, --privatekeybase64=VALUE          |  no                      |                 |
| --pk, --privatekeyfile=VALUE            |  no                      |                 |
| --cp, --certpassword=VALUE              |  no                      |                 |
| --tb, --addtrustedcertbase64=VALUE      |  no                      |                 |
| --tf, --addtrustedcertfile=VALUE        |  no                      |                 |
| --ib, --addissuercertbase64=VALUE       |  no                      |                 |
| --if, --addissuercertfile=VALUE         |  no                      |                 |
| --rb, --updatecrlbase64=VALUE           |  no                      |                 |
| --uc, --updatecrlfile=VALUE             |  no                      |                 |
| --rc, --removecert=VALUE                |  no                      |                 |
| --dt, --devicecertstoretype=VALUE       |  no                      |                 |
| --dp, --devicecertstorepath=VALUE       |  no                      |                 |
| -i, --install                           |  no                      |                 |
| -h, --help                              |  yes                     |                 |
| --st, --opcstacktracemask=VALUE         |  no                      |                 |
| --sd, --shopfloordomain=VALUE           |  no                      |  same as --s, --site option               |
| --vc, --verboseconsole=VALUE            |  no                      |                 |
| --as, --autotrustservercerts=VALUE      |  no                      |  same as --aa, --autoaccept               |
| --tt, --trustedcertstoretype=VALUE      |  no                      |  env variable TrustedPeerCertificatesType=VALUE  |
| --rt, --rejectedcertstoretype=VALUE     |  no                      |  env variable RejectedCertificateStoreType=VALUE   |
| --it, --issuercertstoretype=VALUE       |  no                      |  env variable TrustedIssuerCertificatesType=VALUE  |

## OPC UA Certificates management

The Azure Industrial IoT platform connects to OPC UA servers built into machines or industrial systems via OPC UA client/server. There is an OPC UA client built into the OPC Publisher Edge module. OPC UA Client/server uses an OPC UA Secure Channel to secure this connection. The OPC UA Secure Channel in turn uses X.509 certificates to establish trust between the client and the server. This is done through mutual authentication, i.e. the certificates must be "accepted" (or trusted) by both the client and the server. To simplify setup, the OPC Publisher Edge module has a setting enabled to automatically accept all server certificates ("--aa").

However, the biggest hurdle most OT admins need to overcome when deploying the Azure Industrial IoT platform is to accept the Azure Industrial IoT platform X.509 certificate in the OPC UA server the platform tries to connect to. There is usually a configuration tool that comes with the built-in OPC UA server where certificates can be trusted.
For example for KepServerEx, configure the trusted Client certificate as discussed [here]( https://www.kepware.com/getattachment/ccefc1a5-9b13-41e6-99d9-2b00cc85373e/opc-ua-client-server-easy-guide.pdf).
To use the [OPC PLC Server Simulator](https://docs.microsoft.com/en-us/samples/azure-samples/iot-edge-opc-plc/azure-iot-sample-opc-ua-server/), be sure to include the “–autoaccept” switch or copy the server.der to the pki.
The pki path can be configured using the `PkiRootPath` command line argument.

By default, the OPC Publisher module will create a self signed x509 certificate with a 1 year expiration. This default, self signed cert includes the Subject Microsoft.Azure.IIoT. This certificate is fine as a demonstration, but for real applications customers may want to use their own certificate.

### Use custom OPC UA application instance certificate in OPC Publisher

One can enable use of CA-signed app certs for OPC Publisher using env variables in both orchestrated and standalone modes.

Besides the `ApplicationCertificateSubjectName`, the `ApplicationName` should be provided as well and needs to be the same value as we have in CN field of the ApplicationCertificateSubjectName like in the example below.

`ApplicationCertificateSubjectName="CN=TEST-PUBLISHER,OU=Windows2019,OU=\"Test OU\",DC=microsoft,DC=com"`

`ApplicationName ="TEST-PUBLISHER"`

## Direct Method compatibility

OPC Publisher version 2.8.2 and above implements [IoT Hub Direct Methods](https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-direct-methods), which can be called from applications using the [IoT Hub Device SDK](https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-sdks).

The direct method request payload of OPC Publisher 2.8.2 and above is backwards compatible with OPC Publisher 2.5.x direct methods. The payload schema allows also configuration of attributes introduced in `pn.json` in OPC Publisher 2.6.x and above (for example: DataSetWriterGroup, DataSetWriterId, QueueSize per node, ...)

**Limitations:** Continuation points for GetConfiguredEndpoints and GetConfiguredNodesOnEndpoint aren't available in 2.8.2

**Note:** The objects and primitives names in the direct method payload api model are camel case formatted in 2.8.2. This follows the guidelines of the rest of the api models through the IIoT Platform. Since the names in json payloads in 2.5.x are pascal case formed, we highly recommend enabling case-insensitive Json parsing in your direct methods based configuration tool. You can find details on json case-insensitive serialization here: [how to enable case-insensitive property name matching with System.Text.Json](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-character-casing), or here: [Newtonsoft Json Serialization Naming Strategy](https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_Serialization_DefaultNamingStrategy.htm)

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
         "monitoredOpcNodesFailedCount": 0,
         "ingressEventNotifications": 0,
         "ingressEvents": 0
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

To retrieve the logs of an IoT Edge module, please check the built-in direct methods provided by IoT Edge `edgeAgent` module [here](https://docs.microsoft.com/azure/iot-edge/how-to-retrieve-iot-edge-logs?view=iotedge-2020-11#retrieve-module-logs).

### GetDiagnosticStartupLog

To get diagnostic info of OPC Publisher, please refer to this [link](https://docs.microsoft.com/azure/iot-edge/how-to-edgeagent-direct-method?view=iotedge-2020-11#diagnostic-direct-methods).

### ExitApplication

If OPC Publisher is in failed state or other unhealthy behavior, you could trigger `RestartModule` direct method to stop and then restart the module. To learn more about this direct method, please have a look at [this](https://docs.microsoft.com/azure/iot-edge/how-to-edgeagent-direct-method?view=iotedge-2020-11) in-built direct method provided by IoT Edge.

### GetInfo

To get the info like version, OS, etc., please refer to the properties provided by IoT Edge which is documented [here](https://docs.microsoft.com/azure/iot-edge/module-edgeagent-edgehub?view=iotedge-2020-11).
